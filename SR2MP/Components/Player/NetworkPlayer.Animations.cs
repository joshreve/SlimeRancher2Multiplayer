namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    public float ReceivedLookY;

    private Transform rightShoulder;
    private Transform rightArmUpper;
    private Transform rightArmLower;
    private Transform rightHand;

    private static UnityEngine.Transform? FindChildRecursive(UnityEngine.Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    private void SetupAnimations()
    {
        rightShoulder = FindChildRecursive(transform, "rClavicleJ");
        rightArmUpper = FindChildRecursive(transform, "rShoulderJ");
        rightArmLower = FindChildRecursive(transform, "rElbowJ");
        rightHand     = FindChildRecursive(transform, "rWristJ");
    }

    private void AnimateArmY()
    {
        if (IsLocal || !hasAnimationController) return;

        if (rightShoulder == null)
        {
            SetupAnimations();
            if (rightShoulder == null) return;
        }

        rightShoulder.localRotation = Quaternion.Euler(320, 180, -ReceivedLookY + 89);
        rightShoulder.localPosition = new Vector3(-0.0612f, -0.1155f, 0.2556f);

        if (rightHand != null)
            rightHand.localRotation = Quaternion.Euler(90, 180, 0);

        if (rightArmUpper != null)
            rightArmUpper.localRotation = Quaternion.identity;

        if (rightArmLower != null)
            rightArmLower.localRotation = Quaternion.Euler(0, 6, 0);
    }
}