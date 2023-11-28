using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class TA_UnityExtensionTools
{
    public static void SafeDestroy(this UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
