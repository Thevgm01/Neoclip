using System;
using UnityEngine;

[Serializable]
public class JointRotationValues
{
    public bool enabled = true;
    
    public ConfigurableJoint[] jointsToOverride;
    
    public RotationDriveMode rotationDriveMode;
    public float positionSpring = 0.0f;
    public float positionDamper = 0.0f;
    public float maximumForce = float.MaxValue;
    public bool useAcceleration = false;
        
    public void Override(ConfigurableJoint joint)
    {
        JointDrive drive = new JointDrive
        {
            positionSpring = positionSpring,
            positionDamper = positionDamper,
            maximumForce = maximumForce,
            useAcceleration = useAcceleration
        };
        
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.angularXDrive = drive;
        joint.angularYZDrive = drive;
        joint.slerpDrive = drive;
    }
}
