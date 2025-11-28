using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SoundManager : NetworkBehaviour
{
    public static SoundManager instance;

    [Header("Footstep Sound References")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float walkingFrequency;
    [SerializeField] private float runningFrequency;
    private float sin;
    private bool isFootstepTriggered;

    [Header("Shooting Sound Refernences")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip dribbleSound;
    [SerializeField] private AudioClip powerShotSound;

    [Header("Movement Sound Reference")]
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip slideSound;

    [Header("Player GK Sound Reference")]
    [SerializeField] private AudioClip catchSound;
    [SerializeField] private AudioClip deflectSound;

    [Header("Ability Sound Reference")]
    [SerializeField] private AudioClip trapSound;
    [SerializeField] private AudioClip rouletteSound;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip swooshSound;
    [SerializeField] private AudioClip dinSound;

    [Header("Crowd Sound Reference")]
    [SerializeField] private AudioClip cheerCrowdSound; // plays when a goal is scored
    [SerializeField] private AudioClip saveCrowdSound; // plays when a big save is made
    [SerializeField] private AudioClip amazedCrowdSound; // plays when trap, roulette, or powershot is played
    [SerializeField] private AudioClip confettiSound;
    [SerializeField] private AudioClip whistleSound;

    [Header("Crowd Ambience Random Interval")]
    [SerializeField] private float minCrowdInterval = 5f;
    [SerializeField] private float maxCrowdInterval = 15f;

    [Header("Crowd Ambience Volumes")]
    [SerializeField] private float normalCrowdVolume = 0.2f;
    [SerializeField] private float mutedCrowdVolume = 0.03f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float playDuration = 18f;

    [Header("Emote Sound Reference")]
    [SerializeField] private AudioClip emote2Sound;

    [Header("UI Sound Reference")]
    [SerializeField] private AudioClip flashTextUISound;

    [Header("Tutorial Sound Reference")]
    [SerializeField] private AudioClip playerDetectorHitSound;

    [Header("Mute Settings")]
    [SerializeField] private float muteDistanceFromEmotes = 50;

    [Header("Local AudioSource References")]
    [SerializeField] private AudioSource localCrowdSoundSource;
    [SerializeField] private AudioSource localFootstepAudioSource;
    [SerializeField] private AudioSource localKickingAudioSource;
    [SerializeField] private AudioSource localAbilityAudioSource;
    [SerializeField] private AudioSource localEmoteAudioSource;
    [SerializeField] private AudioSource localCrowdAmbienceAudioSource;
     
    [Header("Global AudioSource References")]
    [SerializeField] private AudioSource globalFootstepAudioSource;
    [SerializeField] private AudioSource globalKickingAudioSource;
    [SerializeField] private AudioSource globalAbilityAudioSource;
    [SerializeField] private AudioSource globalEmoteAudioSource;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            DisableLocalAudioSourcesForNonOwners();
            return;
        }

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
            Destroy(gameObject);

        // disable user's global audio sources
        globalFootstepAudioSource.enabled = false;
        globalKickingAudioSource.enabled = false;
        globalAbilityAudioSource.enabled = false;

        localCrowdAmbienceAudioSource.volume = normalCrowdVolume;

        base.OnNetworkSpawn();
    }

    private IEnumerator Start()
    {
        if (!IsOwner)
            yield break;

        // wait until ServerManager.instance is ready
        yield return new WaitUntil(() => ServerManager.instance != null);

        if (ServerManager.instance.isPracticeServer || ServerManager.instance.isTutorialServer)
            localCrowdAmbienceAudioSource.enabled = false;
    }

    private void Update()
    {
        if (!IsOwner || PlayerMovement.instance.isMovementDisabled || HandleCursorSettings.instance.IsUIOn())
            return;

        if ((PlayerMovement.instance.isWalking || PlayerMovement.instance.isSprinting) && PlayerInfo.instance.playingObj.activeInHierarchy)
            PlayFootstepAudio();

        // for when emotes and other things play
        if (!ServerManager.instance.isPracticeServer && !ServerManager.instance.isTutorialServer)
            HandleMutingCrowdAmbienceAudioSource();
    }

    #region Non-Crowd Sounds
    private void PlayFootstepAudio()
    {
        sin = Mathf.Sin(Time.time * (PlayerMovement.instance.isWalking ? walkingFrequency : runningFrequency));
        float pitch = Random.Range(0.9f, 1.21f);
        int ran = Random.Range(0, footstepSounds.Length);

        if (sin > 0.97f && !isFootstepTriggered)
        {
            isFootstepTriggered = true;
            localFootstepAudioSource.pitch = pitch;
            localFootstepAudioSource.PlayOneShot(footstepSounds[ran]);

            // sync
            if (IsHost)
                PlayGlobalFootballStepAudioClientRpc(pitch, ran);
            else
                PlayGlobalFootballStepAudioServerRpc(pitch, ran);
        }

        else if (isFootstepTriggered && sin < -0.97f)
        {
            isFootstepTriggered = false;
        }
    }

    public void PlayFlashTextUISoundEffect() => localCrowdSoundSource.PlayOneShot(flashTextUISound);
    public void PlayShootSoundEffect()
    {
        localKickingAudioSource.PlayOneShot(shootSound);

        // sync
        if (IsHost)
            PlayGlobalShootAudioClientRpc();
        else
            PlayGlobalShootAudioServerRpc();
    }

    public void PlayDribbleSoundEffect()
    {
        localKickingAudioSource.PlayOneShot(dribbleSound);

        // sync
        if (IsHost)
            PlayGlobalDribbleAudioClientRpc();
        else
            PlayGlobalDribbleAudioServerRpc();
    }

    public void PlayPowerShotSoundEffect()
    {
        localKickingAudioSource.PlayOneShot(powerShotSound);

        // sync
        if (IsHost)
            PlayGlobalPowershotAudioClientRpc();
        else
            PlayGlobalPowershotAudioServerRpc();
    }

    public void PlayTrapSoundEffect()
    {
        localKickingAudioSource.PlayOneShot(trapSound);

        // sync
        if (IsHost)
            PlayGlobalDribbleAudioClientRpc();
        else
            PlayGlobalDribbleAudioServerRpc();
    }

    public void PlayRouletteSoundEffect()
    {
        localKickingAudioSource.PlayOneShot(rouletteSound);

        // sync
        if (IsHost)
            PlayRouletteAudioClientRpc();
        else
            PlayRouletteAudioServerRpc();
    }

    public void PlayDashSoundEffect()
    {
        localAbilityAudioSource.PlayOneShot(dashSound);

        // sync
        if (IsHost)
            PlayGlobalDashAudioClientRpc();
        else
            PlayGlobalDashAudioServerRpc();
    }

    public void PlayDingSound() => localAbilityAudioSource.PlayOneShot(dinSound);

    public void PlayPunchSoundEffect()
    {
        localAbilityAudioSource.PlayOneShot(punchSound);

        // sync
        if (IsHost)
            PlayPunchSoundEffectClientRpc();
        else
            PlayPunchSoundEffectServerRpc();
    }

    public void PlaySwooshSoundEffect()
    {
        localAbilityAudioSource.PlayOneShot(swooshSound);

        // sync
        if (IsHost)
            PlaySwooshSoundEffectClientRpc();
        else
            PlaySwooshSoundEffectServerRpc();
    }

    public void PlaySlideSoundEffect(float duration)
    {
        localAbilityAudioSource.clip = slideSound;
        localAbilityAudioSource.Play();

        // sync
        if (IsHost)
            PlayGlobalSlideAudioClientRpc(duration);
        else
            PlayGlobalSlideAudioServerRpc(duration);

        Invoke(nameof(StopSlideSoundEffect), duration);
    }

    private void StopSlideSoundEffect()
    {
        localAbilityAudioSource.Stop();
        localAbilityAudioSource.clip = null;
    }

    public void PlayPlayerGKCatchSoundEffect()
    {
        localAbilityAudioSource.PlayOneShot(catchSound);

        // sync
        if (IsHost)
            PlayCatchSoundEffectClientRpc();
        else
            PlayCatchSoundEffectServerRpc();
    }

    [ServerRpc]
    public void PlayCatchSoundEffectServerRpc() => PlayCatchSoundEffectClientRpc();

    [ClientRpc]
    public void PlayCatchSoundEffectClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(catchSound);
    }

    public void PlayPlayerGKDeflectSoundEffect()
    {
        localKickingAudioSource.PlayOneShot(deflectSound);

        // sync
        if (IsHost)
            PlayDeflectSoundEffectClientRpc();
        else
            PlayDeflectSoundEffectServerRpc();
    }

    [ServerRpc]
    public void PlayDeflectSoundEffectServerRpc() => PlayDeflectSoundEffectClientRpc();

    [ClientRpc]
    public void PlayDeflectSoundEffectClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(deflectSound);
    }

    [ServerRpc]
    public void PlayPunchSoundEffectServerRpc() => PlayPunchSoundEffectClientRpc();

    [ClientRpc]
    public void PlayPunchSoundEffectClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(punchSound);
    }

    [ServerRpc]
    public void PlaySwooshSoundEffectServerRpc() => PlaySwooshSoundEffectClientRpc();

    [ClientRpc]
    public void PlaySwooshSoundEffectClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(swooshSound);
    }

    [ServerRpc]
    public void PlayTrapAudioServerRpc() => PlayTrapAudioClientRpc();

    [ClientRpc]
    public void PlayTrapAudioClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(trapSound);
    }

    [ServerRpc]
    public void PlayRouletteAudioServerRpc() => PlayRouletteAudioClientRpc();

    [ClientRpc]
    public void PlayRouletteAudioClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(rouletteSound);
    }

    [ServerRpc]
    public void PlayGlobalPowershotAudioServerRpc() => PlayGlobalPowershotAudioClientRpc();

    [ClientRpc]
    public void PlayGlobalPowershotAudioClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(powerShotSound);
    }

    [ServerRpc]
    public void PlayGlobalFootballStepAudioServerRpc(float pitch, int ran) => PlayGlobalFootballStepAudioClientRpc(pitch, ran);

    [ClientRpc]
    public void PlayGlobalFootballStepAudioClientRpc(float pitch, int ran)
    {
        if (globalFootstepAudioSource.enabled == false)
            return;

        globalFootstepAudioSource.pitch = pitch;
        globalFootstepAudioSource.PlayOneShot(footstepSounds[ran]);
    }

    [ServerRpc]
    public void PlayGlobalShootAudioServerRpc() => PlayGlobalShootAudioClientRpc();

    [ClientRpc]
    public void PlayGlobalShootAudioClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(shootSound);
    }

    [ServerRpc]
    public void PlayGlobalDribbleAudioServerRpc() => PlayGlobalDribbleAudioClientRpc();

    [ClientRpc]
    public void PlayGlobalDribbleAudioClientRpc()
    {
        if (globalKickingAudioSource.enabled == false)
            return;

        globalKickingAudioSource.PlayOneShot(dribbleSound);
    }

    [ServerRpc]
    public void PlayGlobalDashAudioServerRpc() => PlayGlobalDashAudioClientRpc();

    [ClientRpc]
    public void PlayGlobalDashAudioClientRpc()
    {
        if (globalAbilityAudioSource.enabled == false)
            return;

        globalAbilityAudioSource.PlayOneShot(dashSound);
    }

    [ServerRpc]
    public void PlayGlobalSlideAudioServerRpc(float duration) => PlayGlobalSlideAudioClientRpc(duration);

    [ClientRpc]
    public void PlayGlobalSlideAudioClientRpc(float duration)
    {
        if (globalAbilityAudioSource.enabled == false)
            return;

        globalAbilityAudioSource.clip = slideSound;
        globalAbilityAudioSource.Play();

        Invoke(nameof(StopSlideSoundEffect), duration);
    }

    #endregion

    #region Crowd Sounds

    public void PlayCheerCrowdSound()
    {
        localCrowdSoundSource.PlayOneShot(cheerCrowdSound);
    }

    public void PlaySaveCrowdSound()
    {
        localCrowdSoundSource.PlayOneShot(saveCrowdSound);
    }

    public void PlayAmazedCrowdSound()
    {
        localCrowdSoundSource.PlayOneShot(amazedCrowdSound);
    }

    public void PlayWhistleSound()
    {
        localCrowdSoundSource.PlayOneShot(whistleSound);
    }

    #endregion

    #region Emote Sounds

    public void PlayEmote2Song(bool condition)
    {
        if (condition)
        {
            localEmoteAudioSource.clip = emote2Sound;
            localEmoteAudioSource.Play();
            
            // mute other player's emote sounds
            globalEmoteAudioSource.mute = true;
        }

        else
        {
            localEmoteAudioSource.Stop();

            // allow us to hear other's emote sounds
            globalEmoteAudioSource.mute = false;
        }

        if (IsHost)
            PlaySambaAudioClientRpc(condition);
        else
            PlaySambaAudioServerRpc(condition);
    }

    [ServerRpc]
    private void PlaySambaAudioServerRpc(bool condition) => PlaySambaAudioClientRpc(condition);

    [ClientRpc]
    private void PlaySambaAudioClientRpc(bool condition)
    {
        if (IsOwner)
            return;

        if (condition)
        {
            globalEmoteAudioSource.clip = emote2Sound;
            globalEmoteAudioSource.mute = true;
        }

        else
            globalEmoteAudioSource.mute = false;
    }

    #endregion

    #region Crowd Ambience System

    private void HandleMutingCrowdAmbienceAudioSource()
    {
        float targetVolume = normalCrowdVolume;

        // fade down if too close to an emote
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allAudioSources)
        {
            if (source.gameObject.name == "GlobalEmoteAudioSource")
            {
                float distance = Vector3.Distance(transform.position, source.transform.position);
                if (distance <= muteDistanceFromEmotes)
                    targetVolume = mutedCrowdVolume;
            }
        }

        // fade down if a team scored
        if (ServerManager.instance.didATeamScore.Value)
            targetVolume = 0;

        // fade down if we are playing an emote
        if (localEmoteAudioSource.isPlaying)
            targetVolume = 0;

        // start fading
        AdjustCrowdAmbienceSound(targetVolume);
    }

    private void AdjustCrowdAmbienceSound(float targetVolume)
    {
        localCrowdAmbienceAudioSource.volume = targetVolume;
    }

    #endregion

    #region Tutorial Sound

    public void PlayTutorialPlayerDetectorHitSound() => localKickingAudioSource.PlayOneShot(playerDetectorHitSound); // use kicking audio source cause why not

    private void DisableLocalAudioSourcesForNonOwners()
    {
        localCrowdSoundSource.enabled = false;
        localFootstepAudioSource.enabled = false;
        localKickingAudioSource.enabled = false;
        localAbilityAudioSource.enabled = false;
        localEmoteAudioSource.enabled = false;
        localCrowdAmbienceAudioSource.enabled = false;
    }

    #endregion
}
