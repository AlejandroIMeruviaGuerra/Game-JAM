    using UnityEngine;

    public class KeyRewardOnTTTWin : MonoBehaviour
    {
        [Header("Configuración de recompensa")]
        public string keyID = "LlaveDelSotano";   // cambia al ID real que uses
        public bool giveKey = true;
        [TextArea]
        public string npcWinMessage =
            "¡Ganaste! La llave está detrás del horno, cerca de la salida.";

        private Player player;

        void OnEnable()
        {
            GameEvents.OnTTTWin += HandleWin;
        }

        void OnDisable()
        {
            GameEvents.OnTTTWin -= HandleWin;
        }

        void Start()
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.GetComponent<Player>();
        }

        void HandleWin()
        {
            if (giveKey && player != null)
            {
                // Si tienes clase Key, podrías instanciar una, pero basta con agregar el ID
                if (!player.collectedKeys.Contains(keyID))
                {
                    player.collectedKeys.Add(keyID);
                    Debug.Log($"[TTT] Entregada llave: {keyID}");
                }
            }

            // Aquí puedes disparar un diálogo del NPC, un waypoint, etc.
            Debug.Log($"[TTT] Pista del NPC: {npcWinMessage}");
            // Ejemplo simple de UI temporal:
            // Puedes mostrar un panel con este texto en tu HUD si lo deseas.
        }
    }
