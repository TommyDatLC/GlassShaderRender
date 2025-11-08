using System.Collections;
using TMPro;
using UnityEngine;

namespace GlassShader.Script.URP.SampleSceneURP
{
    public class FrameRateCounter : MonoBehaviour
    {
        private TextMeshProUGUI text;

        private void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            StartCoroutine(Counter());
        }
      
        IEnumerator Counter()
        {
            for (;;)
            {
                float frameRate = 1f / Time.deltaTime;
                text.text = $"FPS {frameRate}" ;
                yield return new  WaitForSeconds(1);
              
            }
        }
        private void Update()
        {
          
        }
    }
}
