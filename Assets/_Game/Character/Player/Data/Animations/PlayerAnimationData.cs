using System;
using UnityEngine;

[Serializable]
public class PlayerAnimationData
{
    [Header("State Group Parameter Names")]
    [SerializeField] private string groundedParameterName = "Grounded";
    [SerializeField] private string stoppingParameterName = "Stopping";
    [SerializeField] private string landingParameterName = "Landing";
    [SerializeField] private string airborneParameterName = "Airborne";


    [Header("Grounded Parameter Names")]
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string dashParameterName = "isDashing";
    [SerializeField] private string mediumStopParameterName = "isMediumStopping";
    [SerializeField] private string hardStopParameterName = "isHardStopping";

    [Header("Airborne Parameter Names")]
    [SerializeField] private string jumpParameterName = "isJumping";
    [SerializeField] private string fallParameterName = "isFalling";
    [SerializeField] private string rollParameterName = "isRolling";
    [SerializeField] private string hardLandParameterName = "isHardLanding";
    
    [Header("Combat Parameter Names")]
    [SerializeField] private string combatParameterName = "isInJumping";
    [SerializeField] private string wantsToAttackParameterName = "wantsToAttack";
    
    
    public int GroundedParameterHash { get; private set; }
    public int StoppingParameterHash { get; private set; }
    public int LandingParameterHash { get; private set; }
    public int AirborneParameterHash { get; private set; }


    public int SpeedParameterHash { get; private set; }
    public int DashParameterHash { get; private set; }
    public int MediumStopParameterHash { get; private set; }
    public int HardStopParameterHash { get; private set; }

    public int JumpParameterHash { get; private set; }
    public int FallParameterHash { get; private set; }
    public int RollParameterHash { get; private set; }
    public int HardLandParameterHash { get; private set; }
    
    public int CombatParameterName { get; private set; }
    public int WantsToAttackParameterName { get; private set; }
    
    public void Initialize()
    {
        GroundedParameterHash = Animator.StringToHash(groundedParameterName);
        StoppingParameterHash = Animator.StringToHash(stoppingParameterName);
        LandingParameterHash = Animator.StringToHash(landingParameterName);
        AirborneParameterHash = Animator.StringToHash(airborneParameterName);

        SpeedParameterHash = Animator.StringToHash(speedParameterName);
        DashParameterHash = Animator.StringToHash(dashParameterName);
        MediumStopParameterHash = Animator.StringToHash(mediumStopParameterName);
        HardStopParameterHash = Animator.StringToHash(hardStopParameterName);
        RollParameterHash = Animator.StringToHash(rollParameterName);
        HardLandParameterHash = Animator.StringToHash(hardLandParameterName);
        
        JumpParameterHash = Animator.StringToHash((jumpParameterName));
        FallParameterHash = Animator.StringToHash(fallParameterName);
        
        CombatParameterName = Animator.StringToHash(combatParameterName);
        WantsToAttackParameterName = Animator.StringToHash(wantsToAttackParameterName);
    }
}
