public interface IVRRayInteractable
{
    bool IsInteractable { get; }

    void OnRayHoverEnter();
    void OnRayHoverExit();
    void OnRayClick();
}
