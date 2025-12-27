using UnityEngine;

public class MobileInput : MonoBehaviour
{
    public static MobileInput Instance;

    [HideInInspector] public float horizontal = 0f;
    [HideInInspector] public bool jump = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PointerDownLeft()
    {
        horizontal = -1f;
    }

    public void PointerUpLeft()
    {
        if (horizontal < 0) horizontal = 0f;
    }

    public void PointerDownRight()
    {
        horizontal = 1f;
    }

    public void PointerUpRight()
    {
        if (horizontal > 0) horizontal = 0f;
    }

    public void PointerDownJump()
    {
        jump = true;
    }

    public void PointerUpJump()
    {
        jump = false;
    }
}
