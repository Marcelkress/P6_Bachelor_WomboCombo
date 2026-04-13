using UnityEngine;
using Event = AK.Wwise.Event;

public class MonsterAudio : MonoBehaviour
{
    [SerializeField] private AK.Wwise.Event attackSoundEvent;
    [SerializeField] private AK.Wwise.Event playerDamageSoundEvent;
    [SerializeField] private AK.Wwise.Event enemyDamageSound;

    // This method will be called by the Animation Event
    public void PlayAttackSound()
    {
        attackSoundEvent.Post(gameObject);
    }

    public void PlayerHitSound()
    { 
        playerDamageSoundEvent.Post(gameObject);
        
    }

    public void EnemyHitSound()
    {
        enemyDamageSound.Post(gameObject);
    }
}


