using UnityEngine;

namespace WeirdBrothers
{
    public partial class PlayerController : MonoBehaviour
    {
        [Space]
        [Header("Touch Inputs")]
        [SerializeField]
        public float touchSensitivity = 10f;

        [SerializeField]
        public float aimSensitivity = 15f;

        private float lookVer, lookHor;
        private float HandleAxisInputDelegate(string axisName)
        {
            switch (axisName)
            {

                case "Mouse X":

                    if (Input.touchCount > 0)
                    {
                        return TouchLook.TouchDist.x / touchSensitivity;
                    }
                    else
                    {
                        return Input.GetAxis(axisName);
                    }

                case "Mouse Y":
                    if (Input.touchCount > 0)
                    {
                        return TouchLook.TouchDist.y / touchSensitivity;
                    }
                    else
                    {
                        return Input.GetAxis(axisName);
                    }

                default:
                    Debug.LogError("Input <" + axisName + "> not recognyzed.", this);
                    break;
            }

            return 0f;
        }        
    }
}