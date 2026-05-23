using UnityEngine;

public class BossBehavior : EnemyBehavior
{
    [Header("Phase Behaviors")]
    [SerializeField] private ChaseBehavior chaseBehavior;
    [SerializeField] private RangedBehavior rangedBehavior;
    [SerializeField] private JumperBehavior jumperBehavior;

    [Header("Health Thresholds")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float phase2Threshold = 2f;
    [SerializeField] private float phase3Threshold = 1f;

    [Header("Current Health")]
    [SerializeField] private float currentHealth;

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            UpdatePhase();
        }
    }

    private EnemyBehavior currentBehavior;

    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3
    }

    private BossPhase currentPhase = (BossPhase)(-1);

    public override void HandleBehavior(bool canSeePlayer)
    {
        UpdatePhase();

        if (currentBehavior != null)
        {
            currentBehavior.HandleBehavior(canSeePlayer);
        }
    }

    public override void Initialize(EnemyCore coreRef)
    {
        base.Initialize(coreRef);

        currentHealth = maxHealth;

        //Initialize all behaviors with same core
        chaseBehavior.Initialize(coreRef);
        rangedBehavior.Initialize(coreRef);
        jumperBehavior.Initialize(coreRef);

        SetPhase(BossPhase.Phase1);
    }

    private void UpdatePhase()
    {
        if (currentHealth <= phase3Threshold)
        {
            SetPhase(BossPhase.Phase3);
        }
        else if (currentHealth <= phase2Threshold)
        {
            SetPhase(BossPhase.Phase2);
        }
        else
        {
            SetPhase(BossPhase.Phase1);
        }
    }

    private void SetPhase(BossPhase newPhase)
    {
        if (newPhase == currentPhase)
            return;

        currentPhase = newPhase;

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                currentBehavior = chaseBehavior;
                break;

            case BossPhase.Phase2:
                currentBehavior = rangedBehavior;
                break;

            case BossPhase.Phase3:
                currentBehavior = jumperBehavior;
                break;
        }
    }

    //Basic damage system
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}