using UnityEngine;

public abstract class Selectable : MonoBehaviour
{
    /// <summary>
    /// Wird aufgerufen, wenn das Objekt ausgewählt oder abgewählt wird.
    /// </summary>
    public abstract void SetSelected(bool selected);

    /// <summary>
    /// Gibt den Anzeigenamen des Objekts zurück (z. B. für Tooltip, UI).
    /// </summary>
    public virtual string GetName()
    {
        return gameObject.name;
    }
}
