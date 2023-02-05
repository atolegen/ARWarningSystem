using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

namespace ForRayCastMask {
    public class ForRaycastMask : MonoBehaviour
    {
        private TextMeshProUGUI text;
        // Start is called before the first frame update
        void Start()
        {
            text = GetComponent<TextMeshProUGUI>(); // "I love you";//GetPositionOnSpatialMap(2).x.ToString() + "___" + GetPositionOnSpatialMap(2).y.ToString() + "____" + GetPositionOnSpatialMap(2).z.ToString();
            text.text = "I love you";
        }

    }
}