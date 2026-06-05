using UnityEngine;

namespace Saglik360.UI
{
    /// <summary>
    /// Canvas'ı oyun başladığında oyuncunun tam önüne ışınlar ve orada sabit bırakır.
    /// Böylece UI kafayı takip etmez ama hep doğru yerde başlar.
    /// </summary>
    public class UIAlignWithPlayerOnStart : MonoBehaviour
    {
        [Tooltip("UI'ın kameradan kaç metre önde duracağı")]
        public float distance = 2.0f;
        
        [Tooltip("UI'ın kameraya göre yükseklik farkı (0 = tam göz hizası)")]
        public float heightOffset = -0.2f;

        private void Start()
        {
            // VR kaskının yerini bulması bazen birkaç salise sürebilir,
            // bu yüzden pozisyonlamayı çok kısa bir gecikmeyle yapıyoruz.
            Invoke(nameof(AlignWithPlayer), 0.1f);
        }

        private void AlignWithPlayer()
        {
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null) return;

            // Sadece yatay düzlemdeki (X ve Z) bakış yönünü alıyoruz
            // Böylece oyuncu kafasını aşağı/yukarı eğmiş olsa bile menü dik durur.
            Vector3 forward = cam.forward;
            forward.y = 0; 
            
            if (forward.sqrMagnitude < 0.001f) 
                forward = Vector3.forward;
            
            forward.Normalize();

            // Pozisyonu hesapla: Kameranın pozisyonu + (İleri Yön * Mesafe)
            Vector3 newPos = cam.position + (forward * distance);
            newPos.y = cam.position.y + heightOffset; // Yüksekliği ayarla
            
            transform.position = newPos;

            // Menüyü oyuncuya doğru döndür
            transform.rotation = Quaternion.LookRotation(forward);
            
            Debug.Log($"[UIAlign] Canvas oyuncunun önüne yerleştirildi. Pozisyon: {newPos}");
        }
    }
}
