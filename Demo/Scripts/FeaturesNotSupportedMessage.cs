using UnityEngine;
using UnityEngine.UI;

namespace VLB.Samples
{
    [RequireComponent(typeof(Text))]
    public class FeaturesNotSupportedMessage : MonoBehaviour
    {
        void Start()
        {
            var textUI = GetComponent<Text>();
            Debug.Assert(textUI);
            textUI.text = Noise3D.isSupported ? "" : Noise3D.isNotSupportedString;
        }
    }
}
