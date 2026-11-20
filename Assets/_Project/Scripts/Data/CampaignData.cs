using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardiaoDoFarol
{
    /// <summary>
    /// Um herói aposentado que visita a taverna em um determinado dia.
    /// </summary>
    [Serializable]
    public class CustomerProfile
    {
        public string heroName = "Herói";

        [Tooltip("Sprite opcional do herói. Se ficar vazio, o sprite do prefab de cliente é mantido.")]
        public Sprite sprite;

        [Tooltip("Quadros de caminhada (opcional). Se vazio, o herói desliza sem animar.")]
        public Sprite[] walkFrames = new Sprite[0];

        [Tooltip("Cor aplicada ao sprite. Use branco (1,1,1,1) quando estiver usando sprites próprios.")]
        public Color tint = Color.white;

        [Tooltip("Falas exibidas, em sequência, quando o herói é atendido corretamente.")]
        [TextArea(2, 4)] public string[] servedLines = new string[0];
    }

    /// <summary>
    /// Parâmetros de um dia da campanha.
    /// </summary>
    [Serializable]
    public class DayConfig
    {
        public string title = "Dia";

        [Header("Expediente")]
        [Tooltip("Duração do expediente em segundos.")]
        [Min(10f)] public float shiftDuration = 100f;

        [Tooltip("Espera antes do primeiro cliente chegar (segundos).")]
        [Min(0f)] public float firstArrivalDelay = 2f;

        [Tooltip("Intervalo mínimo entre chegadas (segundos).")]
        [Min(0.5f)] public float minArrivalInterval = 8f;

        [Tooltip("Intervalo máximo entre chegadas (segundos).")]
        [Min(0.5f)] public float maxArrivalInterval = 12f;

        [Tooltip("Tempo de paciência de cada cliente sentado (segundos).")]
        [Min(5f)] public float patienceSeconds = 45f;

        [Header("Narrativa do início do dia")]
        public string narratorName = "Taverneiro";
        [TextArea(2, 4)] public string[] introLines = new string[0];

        [Header("Clientes (a quantidade de clientes do dia é o tamanho desta lista)")]
        public List<CustomerProfile> customers = new List<CustomerProfile>();

        public int CustomerCount => customers != null ? customers.Count : 0;
    }

    /// <summary>
    /// Toda a campanha de 5 dias: parâmetros, clientes, falas e textos dos finais.
    /// Edite o asset "CampanhaGuardiao" no Inspector para alterar textos e balanceamento.
    /// </summary>
    [CreateAssetMenu(fileName = "CampanhaGuardiao", menuName = "Guardiao do Farol/Campanha")]
    public class CampaignData : ScriptableObject
    {
        public List<DayConfig> days = new List<DayConfig>();

        [Header("Final de vitória ({moedas} é substituído pelo total)")]
        public string victoryTitle = "A TAVERNA FOI SALVA!";
        [TextArea(4, 10)] public string victoryText = "";

        [Header("Final de derrota ({moedas} é substituído pelo total)")]
        public string defeatTitle = "A TAVERNA FOI FECHADA";
        [TextArea(4, 10)] public string defeatText = "";

        public int DayCount => days != null ? days.Count : 0;

        public int TotalCustomers
        {
            get
            {
                int total = 0;
                if (days == null) return 0;
                foreach (DayConfig day in days)
                {
                    if (day != null) total += day.CustomerCount;
                }
                return total;
            }
        }

        public DayConfig GetDay(int index)
        {
            if (days == null || index < 0 || index >= days.Count) return null;
            return days[index];
        }

        /// <summary>
        /// Cria uma campanha em memória com o conteúdo padrão (usada se nenhum asset for atribuído).
        /// </summary>
        public static CampaignData CreateDefault()
        {
            CampaignData data = CreateInstance<CampaignData>();
            data.name = "CampanhaPadrao";
            data.FillWithDefaultContent();
            return data;
        }

        [ContextMenu("Restaurar conteúdo padrão")]
        public void FillWithDefaultContent()
        {
            Color borin = new Color(0.85f, 0.35f, 0.25f);
            Color lyra = new Color(0.62f, 0.45f, 0.92f);
            Color tavio = new Color(0.40f, 0.78f, 0.38f);
            Color aldric = new Color(0.95f, 0.88f, 0.45f);
            Color mirela = new Color(0.30f, 0.75f, 0.80f);
            Color grom = new Color(0.80f, 0.55f, 0.30f);
            Color celestia = new Color(0.95f, 0.95f, 0.98f);

            days = new List<DayConfig>
            {
                Day("Dia 1", 100f, 2f, 10f, 14f, 50f,
                    new[]
                    {
                        "Mais um dia na taverna O Farol. Dizem que a luz desta casa guia heróis cansados de volta para casa.",
                        "Os aventureiros de antigamente agora só procuram uma cerveja, uma carne assada ou uma sopa quente.",
                        "WASD ou setas para andar. E ou Espaço para pegar no barril ou no fogão e para entregar nas mesas."
                    },
                    Hero("Borin, o Guerreiro", borin,
                        "Ah, isso aqui... quase tão bom quanto a vitória contra o Dragão de Pedra.",
                        "Às vezes eu ainda acordo procurando minha espada. Agora só encontro o travesseiro."),
                    Hero("Lyra, a Maga", lyra,
                        "Antigamente eu esquentava a sopa com um estalar de dedos.",
                        "Minha magia foi embora com a idade. Ainda bem que você não precisa dela para cozinhar."),
                    Hero("Tavio, o Arqueiro", tavio,
                        "Quarenta anos acertando maçãs a cem passos de distância...",
                        "Hoje o único alvo que eu quero é esta cadeira confortável.")),

                Day("Dia 2", 100f, 2f, 9f, 13f, 45f,
                    new[]
                    {
                        "O boato se espalhou: a comida d'O Farol continua sendo a melhor do porto.",
                        "Hoje devem aparecer rostos novos. Ou melhor... rostos antigos, cheios de histórias."
                    },
                    Hero("Dom Aldric, o Paladino", aldric,
                        "Jurei proteger os fracos até o fim dos meus dias.",
                        "O juramento continua valendo. Só que agora eu protejo a mesa do canto."),
                    Hero("Mirela, a Ladina", mirela,
                        "Relaxe, taverneiro. Eu me aposentei. Sua bolsa de moedas está segura.",
                        "...Bom, quase sempre."),
                    Hero("Grom, o Bárbaro", grom,
                        "GROM GOSTA DE COMIDA!",
                        "Grom também gosta de você, taverneiro. Grom voltará.")),

                Day("Dia 3", 140f, 2f, 8f, 12f, 40f,
                    new[]
                    {
                        "Hoje cedo encontrei uma carta presa na porta, com o selo do Coletor Real.",
                        "Ainda não tive coragem de ler tudo... mas o cheiro de problema atravessa o papel."
                    },
                    Hero("Irmã Celestia, a Clériga", celestia,
                        "Que a luz deste lugar nunca se apague.",
                        "Vi uma carta com o selo real no balcão. Está tudo bem por aqui?"),
                    Hero("Borin, o Guerreiro", borin,
                        "Carta do Coletor Real? Hunf. Isso nunca é boa notícia.",
                        "Na minha época, a gente resolvia dívidas com um duelo. Hoje é com papelada."),
                    Hero("Mirela, a Ladina", mirela,
                        "Coletor Real... deixa eu adivinhar: Valdemar Ferrete?",
                        "Conheço aquele sujeito. Ele nunca esquece uma moeda devida. Nem uma."),
                    Hero("Lyra, a Maga", lyra,
                        "Você leu a carta? A taverna deve 100 moedas de impostos atrasados?!",
                        "Eu ajudaria, mas gastei minhas economias em poções contra dor nas costas."),
                    Hero("Tavio, o Arqueiro", tavio,
                        "Ouvi os boatos: se a dívida não for paga, O Farol fecha.",
                        "Sem esta taverna, onde nós, velhos heróis, vamos contar nossas histórias?")),

                Day("Dia 4", 140f, 2f, 7f, 10f, 35f,
                    new[]
                    {
                        "Valdemar Ferrete passou por aqui e pregou um aviso na porta:",
                        "\"Pagamento de 100 moedas até o pôr do sol do quinto dia. Caso contrário, a taverna será fechada.\"",
                        "Faltam só dois dias. Cada cliente conta."
                    },
                    Hero("Dom Aldric, o Paladino", aldric,
                        "Vi o aviso na porta. Um paladino não pode ignorar uma injustiça.",
                        "Não posso erguer minha espada contra a lei... mas posso fazer mais um pedido!"),
                    Hero("Grom, o Bárbaro", grom,
                        "Grom viu homem magro de chapéu pregando papel na porta.",
                        "Grom quis jogar homem magro no mar. Celestia disse que não pode. Então Grom come."),
                    Hero("Irmã Celestia, a Clériga", celestia,
                        "Rezei por você e por esta casa esta manhã.",
                        "E espalhei pela vila que a comida daqui cura qualquer tristeza. Vai vir mais gente!"),
                    Hero("Mirela, a Ladina", mirela,
                        "Valdemar já fechou três tavernas no porto. Ele adora o barulho do cadeado.",
                        "Não dê esse prazer a ele. Junte essas moedas."),
                    Hero("Borin, o Guerreiro", borin,
                        "Amanhã é o grande dia, hein?",
                        "Já enfrentei exércitos inteiros. Você vai enfrentar um cobrador. Vai dar tudo certo!")),

                Day("Dia 5", 140f, 2f, 6f, 9f, 30f,
                    new[]
                    {
                        "Último dia. Ao pôr do sol, Valdemar Ferrete virá buscar as 100 moedas.",
                        "Os velhos heróis prometeram vir. Agora depende de mim e da velocidade das minhas pernas."
                    },
                    Hero("Lyra, a Maga", lyra,
                        "Treinei a noite toda e consegui fazer uma faísca!",
                        "Pequena, mas é magia. Hoje tudo é possível."),
                    Hero("Tavio, o Arqueiro", tavio,
                        "Acertei uma maçã hoje de manhã. A primeira em dez anos.",
                        "É um bom sinal, taverneiro. Hoje ninguém erra o alvo."),
                    Hero("Dom Aldric, o Paladino", aldric,
                        "Hoje vim como cliente e como guardião.",
                        "Se Valdemar tentar algo desonesto, terá que passar por mim."),
                    Hero("Grom, o Bárbaro", grom,
                        "Grom trouxe fome de dois bárbaros!",
                        "Taverna não pode fechar. Onde Grom vai comer?"),
                    Hero("Mirela, a Ladina", mirela,
                        "Fiz uma visitinha ao escritório do Valdemar... só para ler os papéis, juro.",
                        "A dívida é real, mas o prazo também: até o pôr do sol. Corra!"))
            };

            victoryTitle = "A TAVERNA FOI SALVA!";
            victoryText =
                "Ao pôr do sol, Valdemar Ferrete abriu a porta e encontrou {moedas} moedas sobre o balcão... " +
                "e sete heróis aposentados de braços cruzados ao redor dele.\n\n" +
                "Ele contou as moedas duas vezes, assinou o recibo e saiu sem dizer uma palavra. " +
                "Borin ergueu a caneca, Grom bateu na mesa, e Lyra acendeu uma pequena chama, a primeira em muitos anos.\n\n" +
                "A luz d'O Farol continua acesa, guiando heróis cansados de volta para casa.";

            defeatTitle = "A TAVERNA FOI FECHADA";
            defeatText =
                "Ao pôr do sol, Valdemar Ferrete contou {moedas} moedas. Não era o bastante.\n\n" +
                "As portas d'O Farol foram trancadas com corrente e cadeado. " +
                "Os heróis prometeram juntar forças e voltar um dia...\n\n" +
                "Mas, naquela noite, a luz da velha taverna se apagou.";
        }

        private static DayConfig Day(string title, float shift, float firstDelay, float minInterval,
            float maxInterval, float patience, string[] intro, params CustomerProfile[] customers)
        {
            return new DayConfig
            {
                title = title,
                shiftDuration = shift,
                firstArrivalDelay = firstDelay,
                minArrivalInterval = minInterval,
                maxArrivalInterval = maxInterval,
                patienceSeconds = patience,
                narratorName = "Taverneiro",
                introLines = intro,
                customers = new List<CustomerProfile>(customers)
            };
        }

        private static CustomerProfile Hero(string heroName, Color tint, params string[] lines)
        {
            return new CustomerProfile
            {
                heroName = heroName,
                tint = tint,
                servedLines = lines
            };
        }
    }
}
