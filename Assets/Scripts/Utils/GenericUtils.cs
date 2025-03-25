using System;
using UnityEngine;
using Random = System.Random;

public static class GenericUtils
{
    public static int FixedUpdateCount => Mathf.CeilToInt(Time.fixedTime / Time.fixedDeltaTime);
    
    public static float ExpT(float speed) => 1.0f - Mathf.Exp(-speed * Time.deltaTime);

    public static int ToLayerNumber(this LayerMask mask) => Mathf.RoundToInt(Mathf.Log(mask.value, 2.0f));
    
    public static Vector2 Rotate(this Vector2 v, float radians)
    {
	    float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
	    return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // https://discussions.unity.com/t/randomize-array-in-c/443241/6
    // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
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
