using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Gera os clientes do dia pela porta, em intervalos, sempre em mesas livres,
    /// e controla quantos ainda estão na taverna.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [SerializeField] private CustomerController customerPrefab;

        [Tooltip("Ponto da porta: onde os clientes aparecem e por onde saem.")]
        [SerializeField] private Transform doorPoint;

        [Tooltip("Objeto pai dos clientes gerados (opcional, só organização).")]
        [SerializeField] private Transform customersParent;

        [SerializeField] private List<Table> tables = new List<Table>();

        [Tooltip("Se não houver mesa livre, tenta novamente após este tempo (segundos).")]
        [Min(0.1f)] [SerializeField] private float retryWhenFullDelay = 1f;

        public event Action<CustomerController> CustomerServed;
        public event Action<CustomerController> CustomerLost;

        public int ActiveCustomerCount => activeCustomers.Count;
        public int PendingCustomerCount => pendingCustomers.Count;
        public bool AllCustomersSpawned => pendingCustomers.Count == 0;

        private readonly Queue<CustomerProfile> pendingCustomers = new Queue<CustomerProfile>();
        private readonly List<CustomerController> activeCustomers = new List<CustomerController>();
        private readonly List<Table> freeTablesBuffer = new List<Table>();
        private DayConfig currentDay;
        private bool spawning;
        private float spawnTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Limpa a taverna e enfileira os clientes do dia (sem começar a gerar).</summary>
        public void PrepareDay(DayConfig day)
        {
            spawning = false;
            currentDay = day;
            pendingCustomers.Clear();

            for (int i = activeCustomers.Count - 1; i >= 0; i--)
            {
                if (activeCustomers[i] != null) Destroy(activeCustomers[i].gameObject);
            }
            activeCustomers.Clear();

            foreach (Table table in tables)
            {
                if (table != null) table.ForceRelease();
            }

            if (day != null && day.customers != null)
            {
                foreach (CustomerProfile profile in day.customers)
                {
                    pendingCustomers.Enqueue(profile ?? new CustomerProfile());
                }
            }
        }

        public void BeginSpawning()
        {
            if (currentDay == null) return;
            spawning = true;
            spawnTimer = currentDay.firstArrivalDelay;
        }

        /// <summary>Para de gerar clientes. Retorna quantos não chegaram a entrar.</summary>
        public int StopSpawning()
        {
            spawning = false;
            int remaining = pendingCustomers.Count;
            pendingCustomers.Clear();
            return remaining;
        }

        private void Update()
        {
            if (!spawning || pendingCustomers.Count == 0 || currentDay == null) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;

            Table table = GetRandomFreeTable();
            if (table == null)
            {
                spawnTimer = retryWhenFullDelay;
                return;
            }

            SpawnCustomer(pendingCustomers.Dequeue(), table);

            float min = Mathf.Min(currentDay.minArrivalInterval, currentDay.maxArrivalInterval);
            float max = Mathf.Max(currentDay.minArrivalInterval, currentDay.maxArrivalInterval);
            spawnTimer = UnityEngine.Random.Range(min, max);
        }

        private Table GetRandomFreeTable()
        {
            freeTablesBuffer.Clear();
            foreach (Table table in tables)
            {
                if (table != null && table.IsFree) freeTablesBuffer.Add(table);
            }

            if (freeTablesBuffer.Count == 0) return null;
            return freeTablesBuffer[UnityEngine.Random.Range(0, freeTablesBuffer.Count)];
        }

        private void SpawnCustomer(CustomerProfile profile, Table table)
        {
            if (customerPrefab == null)
            {
                Debug.LogError("CustomerManager: 'Customer Prefab' não foi atribuído.");
                spawning = false;
                pendingCustomers.Clear();
                return;
            }

            Vector3 door = doorPoint != null ? doorPoint.position : transform.position;
            CustomerController customer = Instantiate(customerPrefab, door, Quaternion.identity, customersParent);
            table.Reserve(customer);

            ProductType order = OrderManager.Instance != null ? OrderManager.Instance.GetRandomOrder() : ProductType.Cerveja;
            customer.Initialize(profile, table, order, currentDay.patienceSeconds, door, this);
            activeCustomers.Add(customer);

            AudioManager.Play(SoundEffect.CustomerEnter);
        }

        public void NotifyServed(CustomerController customer)
        {
            CustomerServed?.Invoke(customer);
        }

        public void NotifyLost(CustomerController customer)
        {
            CustomerLost?.Invoke(customer);
        }

        public void NotifyExited(CustomerController customer)
        {
            activeCustomers.Remove(customer);
        }
    }
}
