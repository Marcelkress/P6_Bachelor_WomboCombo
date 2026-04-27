using UnityEngine;
using Event = AK.Wwise.Event;

public class BossSound : MonoBehaviour
{
   
    [SerializeField] private AK.Wwise.Event bossLaugh;
    [SerializeField] private AK.Wwise.Event bossAttack;
    
    //[SerializeField] private AK.Wwise.Event walkStoneSound;

    // This method will be called by the Animation Event
   

    public void BossLaugh()
    {
        bossLaugh.Post(gameObject);
    }

    public void BossAttack()
    {
        bossAttack.Post(gameObject);
        
    }

}


