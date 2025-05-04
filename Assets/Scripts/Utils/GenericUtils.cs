using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

public static class GenericUtils
{
    public static int FixedUpdateCount => Mathf.CeilToInt(Time.fixedTime / Time.fixedDeltaTime);
    
    /// <summary>
    /// Calculate t for lerps based on an exponential decay function, able to be run every frame.
    /// </summary>
    /// <seealso href="https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/"/>
    /// <param name="speed"></param>
    /// <returns></returns>
    public static float ExpT(float speed) => 1.0f - Mathf.Exp(-speed * Time.deltaTime);

    public static int ToLayerNumber(this LayerMask mask) => Mathf.RoundToInt(Mathf.Log(mask.value, 2.0f));
    
    public static Vector2 Rotate(this Vector2 v, float radians)
    {
	    float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
	    return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    
    /// <summary>
    /// Shuffle the array using the Fisher–Yates algorithm and UnityEngine.Random.
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle"/>
    /// <seealso href="https://discussions.unity.com/t/randomize-array-in-c/443241/6"/>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(this T[] array)
    {
	    for (int t = 0; t < array.Length; t++ )
	    {
		    T tmp = array[t];
		    int r = UnityEngine.Random.Range(t, array.Length);
		    array[t] = array[r];
		    array[r] = tmp;
	    }
    }
    
    /// <summary>
    /// Shuffle the list using the Fisher–Yates algorithm and UnityEngine.Random.
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle"/>
    /// <seealso href="https://discussions.unity.com/t/randomize-array-in-c/443241/6"/>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(this List<T> list)
    {
	    for (int t = 0; t < list.Count; t++ )
	    {
		    T tmp = list[t];
		    int r = UnityEngine.Random.Range(t, list.Count);
		    list[t] = list[r];
		    list[r] = tmp;
	    }
    }
	
    /// <summary>
    /// Starting with the child transform, recursively goes up the hierarchy until it finds the specified parent transform.
    /// </summary>
    /// <param name="child"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static bool IsChildOf(Transform child, Transform parent)
    {
	    Transform temp = child;
	    while (temp != null && temp != parent)
	    {
		    temp = temp.parent;
	    }
	    return temp == parent;
    }

    /// <summary>
    /// As <see cref="IsChildOf"/>, but can match any parent transform of an array.
    /// </summary>
    /// <param name="child"></param>
    /// <param name="parents"></param>
    /// <returns></returns>
    public static bool IsChildOfAny(Transform child, Transform[] parents)
    {
	    Transform temp = child;
	    while (temp != null)
	    {
		    foreach (Transform parent in parents)
		    {
			    if (temp == parent)
			    {
				    return true;
			    }
		    }
		    temp = temp.parent;
	    }
	    return false;
    }
    
    public static void TryDestroyObject(Object obj)
    {
	    if (obj != null)
	    {
		    Object.Destroy(obj);
	    }
    }
    
    public static void TryDestroyObjects(Object[] objs)
    {
	    foreach (Object obj in objs)
	    {
		    TryDestroyObject(obj);
	    }
    }
    
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
	    if (gameObject.TryGetComponent(out T component))
	    {
		    return component;
	    }
	    return gameObject.AddComponent<T>();
    }
    
    public static void CopyConfigurableJointValues(ConfigurableJoint source, ConfigurableJoint target)
    {
	    target.connectedBody = source.connectedBody;
	    target.anchor = source.anchor;
	    target.axis = source.axis;
	    target.autoConfigureConnectedAnchor = source.autoConfigureConnectedAnchor;
	    target.connectedAnchor = source.connectedAnchor;
	    target.secondaryAxis = source.secondaryAxis;
	    target.xMotion = source.xMotion;
	    target.yMotion = source.yMotion;
	    target.zMotion = source.zMotion;
	    target.angularXMotion = source.angularXMotion;
	    target.angularYMotion = source.angularYMotion;
	    target.angularZMotion = source.angularZMotion;
	    target.linearLimitSpring = source.linearLimitSpring;
	    target.linearLimit = source.linearLimit;
	    target.angularXLimitSpring = source.angularXLimitSpring;
	    target.lowAngularXLimit = source.lowAngularXLimit;
	    target.highAngularXLimit = source.highAngularXLimit;
	    target.angularYZDrive = source.angularYZDrive;
	    target.angularYLimit = source.angularYLimit;
	    target.angularZLimit = source.angularZLimit;
	    target.targetPosition = source.targetPosition;
	    target.targetVelocity = source.targetVelocity;
		target.xDrive = source.xDrive;
		target.yDrive = source.yDrive;
		target.zDrive = source.zDrive;
		target.targetRotation = source.targetRotation;
		target.targetAngularVelocity = source.targetAngularVelocity;
		target.rotationDriveMode = source.rotationDriveMode;
		target.angularXDrive = source.angularXDrive;
		target.angularYZDrive = source.angularYZDrive;
		target.slerpDrive = source.slerpDrive;
		target.projectionMode = source.projectionMode;
		target.projectionDistance = source.projectionDistance;
		target.projectionAngle = source.projectionAngle;
		target.configuredInWorldSpace = source.configuredInWorldSpace;
		target.swapBodies = source.swapBodies;
		target.breakForce = source.breakForce;
		target.breakTorque = source.breakTorque;
		target.enableCollision = source.enableCollision;
		target.enablePreprocessing = source.enablePreprocessing;
		target.massScale = source.massScale;
		target.connectedMassScale = source.connectedMassScale;
    }
}
