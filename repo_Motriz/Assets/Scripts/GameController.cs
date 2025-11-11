using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private BodyMotionTracker bodyTracker;
    
    void Start()
    {
        // Si no asignaste el tracker en el Inspector, búscalo
        if (bodyTracker == null)
            bodyTracker = FindObjectOfType<BodyMotionTracker>();
        
        // Suscríbete a los eventos
        bodyTracker.OnLeftFootDetected += OnLeftFootEntered;
        bodyTracker.OnRightFootDetected += OnRightFootEntered;
    }
    
    private void OnLeftFootEntered()
    {
        Debug.Log("¡Pie izquierdo detectado!");
        // Aquí pon tu lógica: sumar puntos, cambiar escena, etc.
    }
    
    private void OnRightFootEntered()
    {
        Debug.Log("¡Pie derecho detectado!");
        // Aquí pon tu lógica
    }
    
    void OnDestroy()
    {
        // Desuscríbete para evitar errores
        if (bodyTracker != null)
        {
            bodyTracker.OnLeftFootDetected -= OnLeftFootEntered;
            bodyTracker.OnRightFootDetected -= OnRightFootEntered;
        }
    }
}