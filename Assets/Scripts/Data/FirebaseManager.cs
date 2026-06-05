// ============================================================
//  Sağlık360 – FirebaseManager.cs  |  Data
// ============================================================
//  Handles all communication with Firebase Realtime Database
//  and Firebase Authentication.
//
//  Dependencies (install via Unity Package Manager):
//    • Firebase SDK for Unity (com.google.firebase.database)
//    • Firebase Auth (com.google.firebase.auth)
//    • Newtonsoft JSON (com.unity.nuget.newtonsoft-json)
//
//  Data schema (mirrors Firebase JSON tree):
//    /sessions/{sessionId}/  → SessionData
//    /patients/{patientId}/  → Patient profile
//    /exercises/             → ExerciseDefinition metadata
// ============================================================

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Saglik360
{
    public class FirebaseManager : MonoBehaviour
    {
        // ── Inspector config ─────────────────────────────────
        [Header("Firebase Config")]
        [Tooltip("Your Firebase Realtime Database URL (e.g. https://saglik360-xxxx.firebaseio.com)")]
        [SerializeField] private string databaseUrl = "https://saglik360-default-rtdb.firebaseio.com";

        [Header("Auth")]
        [Tooltip("Current authenticated user UID (set after login).")]
        public string CurrentUserId { get; private set; }
        public bool   IsAuthenticated => !string.IsNullOrEmpty(CurrentUserId);

        // ── Offline queue ────────────────────────────────────
        // When no internet is available, we cache pending uploads
        // in PlayerPrefs and retry on next launch.
        private const string OfflineQueueKey = "FirebaseOfflineQueue";

        // ── Events ───────────────────────────────────────────
        public event Action<bool>   OnUploadComplete;    // success flag
        public event Action<string> OnAuthStateChanged;  // userId or ""

        // ────────────────────────────────────────────────────
        #region Authentication
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Sign in with email + password via Firebase REST Auth API.
        /// On success, stores the UID for subsequent DB calls.
        /// </summary>
        public void SignIn(string email, string password, Action<bool, string> callback)
        {
            StartCoroutine(SignInCoroutine(email, password, callback));
        }

        private IEnumerator SignInCoroutine(string email, string password, Action<bool, string> callback)
        {
            // Firebase REST sign-in endpoint
            string apiKey  = GetFirebaseApiKey();
            string url     = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
            string payload = JsonConvert.SerializeObject(new { email, password, returnSecureToken = true });

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var resp       = JsonConvert.DeserializeAnonymousType(req.downloadHandler.text, new { localId = "", idToken = "" });
                CurrentUserId  = resp.localId;
                PlayerPrefs.SetString("PatientId",   CurrentUserId);
                PlayerPrefs.SetString("FirebaseToken", resp.idToken);
                OnAuthStateChanged?.Invoke(CurrentUserId);
                callback?.Invoke(true, CurrentUserId);
                Debug.Log($"[FirebaseManager] Giriş başarılı: {CurrentUserId}");
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Giriş hatası: {req.error}");
                callback?.Invoke(false, req.error);
            }
        }

        public void SignOut()
        {
            CurrentUserId = "";
            PlayerPrefs.DeleteKey("FirebaseToken");
            OnAuthStateChanged?.Invoke("");
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Session Upload
        // ────────────────────────────────────────────────────

        /// <summary>Upload a completed SessionData to Firebase.</summary>
        public void UploadSession(Core.SessionData session)
        {
            if (session == null) return;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                QueueOffline(session);
                Debug.LogWarning("[FirebaseManager] İnternet yok. Oturum verisi yerel kuyruğa alındı.");
                return;
            }

            StartCoroutine(UploadSessionCoroutine(session));
        }

        private IEnumerator UploadSessionCoroutine(Core.SessionData session)
        {
            string token   = PlayerPrefs.GetString("FirebaseToken", "");
            string path    = $"/sessions/{CurrentUserId}/{session.SessionId}.json?auth={token}";
            string url     = databaseUrl + path;
            string payload = JsonConvert.SerializeObject(session, Formatting.None,
                                 new JsonSerializerSettings { DateFormatString = "o" });

            using var req = new UnityWebRequest(url, "PUT");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            bool success = req.result == UnityWebRequest.Result.Success;
            OnUploadComplete?.Invoke(success);

            if (success)
                Debug.Log($"[FirebaseManager] Oturum yüklendi: {session.SessionId}");
            else
                Debug.LogError($"[FirebaseManager] Yükleme hatası: {req.error}");
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Patient Profile Read
        // ────────────────────────────────────────────────────

        /// <summary>Fetch patient profile including assigned exercises.</summary>
        public void FetchPatientProfile(string patientId, Action<PatientProfile> callback)
        {
            StartCoroutine(FetchPatientCoroutine(patientId, callback));
        }

        private IEnumerator FetchPatientCoroutine(string patientId, Action<PatientProfile> callback)
        {
            string token = PlayerPrefs.GetString("FirebaseToken", "");
            string url   = $"{databaseUrl}/patients/{patientId}.json?auth={token}";

            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var profile = JsonConvert.DeserializeObject<PatientProfile>(req.downloadHandler.text);
                callback?.Invoke(profile);
                Debug.Log($"[FirebaseManager] Hasta profili alındı: {patientId}");
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Profil alınamadı: {req.error}");
                callback?.Invoke(null);
            }
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Offline Queue
        // ────────────────────────────────────────────────────

        private void QueueOffline(Core.SessionData session)
        {
            string json    = JsonConvert.SerializeObject(session);
            string current = PlayerPrefs.GetString(OfflineQueueKey, "[]");
            var    list    = JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(current);
            list.Add(json);
            PlayerPrefs.SetString(OfflineQueueKey, JsonConvert.SerializeObject(list));
            PlayerPrefs.Save();
        }

        /// <summary>Call on app start when internet is available to flush queued sessions.</summary>
        public void FlushOfflineQueue()
        {
            string raw = PlayerPrefs.GetString(OfflineQueueKey, "[]");
            var list   = JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(raw);

            if (list.Count == 0) return;

            Debug.Log($"[FirebaseManager] {list.Count} bekleyen oturum yükleniyor...");
            foreach (string json in list)
            {
                var session = JsonConvert.DeserializeObject<Core.SessionData>(json);
                UploadSession(session);
            }

            PlayerPrefs.SetString(OfflineQueueKey, "[]");
            PlayerPrefs.Save();
        }

        #endregion

        // ────────────────────────────────────────────────────
        #region Helpers
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Store your Firebase Web API key in a Resources/firebase_config.txt
        /// or inject via a build script. Never hard-code it here.
        /// </summary>
        private string GetFirebaseApiKey()
        {
            TextAsset asset = Resources.Load<TextAsset>("firebase_config");
            if (asset == null)
            {
                Debug.LogError("[FirebaseManager] firebase_config.txt bulunamadı (Resources klasörüne ekleyin).");
                return "";
            }
            return asset.text.Trim();
        }

        #endregion
    }

    // ── Minimal patient profile model ────────────────────────
    [Serializable]
    public class PatientProfile
    {
        public string PatientId;
        public string FullName;
        public string Diagnosis;
        public string TherapistId;
        public string[] AssignedExerciseIds;
    }
}
