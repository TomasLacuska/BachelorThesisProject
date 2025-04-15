using UnityEngine;

public class Farm : MonoBehaviour
{
    // Reference to the BioBurner that reserved this farm (if any).
    private BioBurner reservedBy = null;


    // Returns whether this farm is already reserved.
    public bool IsReserved()
    {
        return reservedBy != null;
    }


    // Reserves this farm for the given burner.
    public void Reserve(BioBurner burner)
    {
        reservedBy = burner;
    }


    // Releases the reservation.
    public void Release()
    {
        reservedBy = null;
    }
}
