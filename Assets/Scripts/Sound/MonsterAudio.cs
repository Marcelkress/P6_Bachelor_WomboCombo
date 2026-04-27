using UnityEngine;
using Event = AK.Wwise.Event;

public class MonsterAudio : MonoBehaviour
{
    [SerializeField] private AK.Wwise.Event attackSoundEvent;
    [SerializeField] private AK.Wwise.Event playerDamageSoundEvent;
    [SerializeField] private AK.Wwise.Event enemyDamageSound;
    [SerializeField] private AK.Wwise.Event spellHitsound;
    [SerializeField] private AK.Wwise.Event minotauserWalkSound;
    
    
    
    //[SerializeField] private AK.Wwise.Event walkStoneSound;

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
        spellHitsound.Post(gameObject);
    }

    public void MinitauserWalk()
    {
        minotauserWalkSound.Post(gameObject);
    }

  

}


