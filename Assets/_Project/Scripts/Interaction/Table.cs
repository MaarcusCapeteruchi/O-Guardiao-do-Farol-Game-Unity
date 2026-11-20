using System.Collections.Generic;
using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Mesa da taverna. Guarda o cliente sentado nela e recebe as entregas do jogador.
    /// O cliente entra pela porta, percorre os waypoints de "Path From Door" em ordem e
    /// termina em "Seat Point". Na saída, faz o caminho inverso.
    /// </summary>
    public class Table : Interactable
    {
        [Tooltip("Onde o cliente fica sentado.")]
        [SerializeField] private Transform seatPoint;

        [Tooltip("Waypoints, em ordem, da porta até a cadeira (sem incluir a porta e a cadeira).")]
        [SerializeField] private List<Transform> pathFromDoor = new List<Transform>();

        public CustomerController Occupant { get; private set; }
        public bool IsFree => Occupant == null;
        public Vector3 SeatPosition => seatPoint != null ? seatPoint.position : transform.position;

        /// <summary>Monta o caminho completo: porta → waypoints → cadeira.</summary>
        public List<Vector3> BuildPathFrom(Vector3 doorPosition)
        {
            List<Vector3> path = new List<Vector3> { doorPosition };
            foreach (Transform point in pathFromDoor)
            {
                if (point != null) path.Add(point.position);
            }
            path.Add(SeatPosition);
            return path;
        }

        public bool Reserve(CustomerController customer)
        {
            if (customer == null || Occupant != null) return false;
            Occupant = customer;
            return true;
        }

        public void Release(CustomerController customer)
        {
            if (Occupant == customer) Occupant = null;
        }

        public void ForceRelease()
        {
            Occupant = null;
        }

        public override bool CanInteract(PlayerInteraction player)
        {
            return player != null && Occupant != null && Occupant.IsWaitingForOrder;
        }

        public override string GetPrompt(PlayerInteraction player)
        {
            if (Occupant == null) return string.Empty;
            string wanted = OrderManager.Instance != null ? OrderManager.Instance.GetName(Occupant.Order) : Occupant.Order.ToString();
            if (player == null || player.CarriedProduct == ProductType.None)
            {
                return Occupant.HeroName + " quer " + wanted;
            }
            return "[E] Entregar para " + Occupant.HeroName + " (quer " + wanted + ")";
        }

        public override void Interact(PlayerInteraction player)
        {
            if (Occupant == null || OrderManager.Instance == null) return;
            OrderManager.Instance.TryDeliver(Occupant, player);
        }

        private void OnDrawGizmos()
        {
            Vector3 previous = transform.position;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            foreach (Transform point in pathFromDoor)
            {
                if (point == null) continue;
                Gizmos.DrawWireSphere(point.position, 0.15f);
            }
            if (seatPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(seatPoint.position, 0.2f);
                Gizmos.DrawLine(previous, seatPoint.position);
            }
        }
    }
}
