using UnityEngine;

namespace CucumberClicker
{
    /// <summary>
    /// Owns the game state and the plain-C# systems. UI scripts read from here and call
    /// its methods; they never change numbers directly.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;

        public GameConfig Config => config;
        public GameState State { get; private set; }
        public Wallet Wallet { get; private set; }
        public Market Market { get; private set; }

        /// <summary>Cucumbers per tap. Upgrade levels arrive in Phase 4.</summary>
        public double TapPower => CostFormula.TapPower(config.baseTapPower, 0, 0);

        /// <summary>Producer output. Producers arrive in Phase 5.</summary>
        public double CucumbersPerSecond => 0;

        public bool HasProducers => CucumbersPerSecond > 0;

        private TapLimiter tapLimiter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (config == null)
            {
                Debug.LogWarning("GameManager has no GameConfig assigned; using defaults.");
                config = GameConfig.CreateDefault();
            }

            Application.targetFrameRate = 60;

            State = new GameState();
            State.stats.firstPlayUtcTicks = TimeService.UtcNowTicks;
            Wallet = new Wallet(State);
            Market = new Market(Wallet, config);
            tapLimiter = new TapLimiter(config.maxTapsPerSecond);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            State.stats.totalPlayTimeSeconds += Time.unscaledDeltaTime;
            if (HasProducers) Wallet.AddCucumbers(CucumbersPerSecond * Time.deltaTime);
        }

        /// <summary>Registers one tap. Returns cucumbers gained, or 0 if the tap was rate-limited.</summary>
        public double Tap()
        {
            if (!tapLimiter.TryTap(Time.unscaledTimeAsDouble)) return 0;

            double gained = TapPower;
            Wallet.AddCucumbers(gained);
            State.stats.totalTaps++;
            return gained;
        }
    }
}
