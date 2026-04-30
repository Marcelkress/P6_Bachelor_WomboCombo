using Unity.VisualScripting;
using UnityEngine;
using Event = AK.Wwise.Event;

public class BossMusic : MonoBehaviour
{
    [SerializeField] private AK.Wwise.Event bossMusic;
    //public GameObject backgroundmusic;
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player"))
        {
            bossMusic.Post(gameObject);
            //Destroy(backgroundmusic.gameObject);
           
        }
    }
    
    
}
