using System.Collections.Generic;
using UnityEngine;

namespace MGSUGE
{
    /// <summary>
    /// Enchainement des contrats (§13.4) et niveau de menace (§10.3). Un seul contrat actif ; quand tous ses ennemis sont
    /// detruits : recompense x (1 + 0,3 x menace), kits de reparation recharges, contrat suivant 5 s plus tard.
    /// Apres le dernier de la liste, la menace augmente d'un cran et le premier recommence.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ContractManager : MonoBehaviour
    {
        public static ContractManager I { get; private set; }
        [Tooltip("coefficients de menace, delais et messages")] public CampaignData campaign;
        [Tooltip("joueur (recoit les credits et les kits)")] public PlayerController player;
        [Tooltip("contrats dans l'ordre (objets de la scene)")] public List<Contract> contracts = new List<Contract>();
        [Tooltip("contrat de depart (0 = le premier)")] [Min(0)] public int startIndex = 0;
        [Tooltip("niveau de menace au demarrage")] [Min(0)] public int startThreat = 0;

        public int Index { get; private set; } = -1;
        public int Count => contracts.Count;
        public Contract Current => Index >= 0 && Index < contracts.Count ? contracts[Index] : null;
        public bool Waiting { get; private set; }
        public float WaitLeft { get; private set; }
        public int Completed { get; private set; }
        public long LastReward { get; private set; }

        void Awake()
        {
            I = this;
            Threat.Data = campaign; Threat.Level = startThreat;
        }

        void Start()
        {
            foreach (var c in contracts) if (c != null) c.Hide();
            if (contracts.Count == 0) return;
            Index = Mathf.Clamp(startIndex, 0, contracts.Count - 1) - 1;
            Waiting = true; WaitLeft = campaign != null ? campaign.firstContractDelay : 1.5f;
        }

        void Update()
        {
            if (contracts.Count == 0) return;
            if (GameManager.I != null && GameManager.I.State == GameState.Title) return; // rien ne demarre sur l'ecran titre
            if (Waiting)
            {
                WaitLeft -= Time.deltaTime;
                if (WaitLeft <= 0f) Next();
                return;
            }
            var c = Current;
            if (c != null && c.Total > 0 && c.Alive == 0) Complete();
        }

        void Complete()
        {
            var c = Current;
            long reward = (long)Mathf.Round((c.data != null ? c.data.reward : 0) * Threat.RewardMult);
            LastReward = reward; Completed++;
            if (player != null)
            {
                player.progress.credits += reward; HudCredits.Gain(reward);
                player.RefillKits();
            }
            string fmt = campaign != null ? campaign.completeMessage : "Contrat terminé — +{0} · kits de réparation rechargés";
            Msg.Post(string.Format(fmt, MathX.Credits(reward)), MsgKind.Good);
            Msg.Big("Contrat terminé", "+" + MathX.Credits(reward), 3f);
            Sfx.Play("buy");
            Waiting = true; WaitLeft = campaign != null ? campaign.nextContractDelay : 5f;
        }

        void Next()
        {
            Waiting = false;
            Index++;
            if (Index >= contracts.Count)
            {
                Index = 0; Threat.Level++;
                string m = campaign != null ? campaign.threatMessage : "Les forces hostiles se renforcent";
                Msg.Post(m, MsgKind.Danger); Sfx.Play("alarm");
            }
            var c = Current; if (c == null) return;
            c.Begin();
            string fmt = campaign != null ? campaign.newContractMessage : "Nouveau contrat : {0}";
            Msg.Post(string.Format(fmt, c.Title), MsgKind.Warning);
            Msg.Big(c.Title, c.data != null ? c.data.description : "", 4.5f);
        }

        /// <summary>Passe immediatement au contrat d'indice donne (tests).</summary>
        public void ForceStart(int index)
        {
            if (Current != null) Current.Hide();
            Index = Mathf.Clamp(index, 0, contracts.Count - 1) - 1; Waiting = true; WaitLeft = 0f;
        }
    }
}
