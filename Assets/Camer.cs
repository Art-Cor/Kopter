using UnityEngine;

public class Move : MonoBehaviour
{   
    public float sens = 1f;
    public float speed = 20;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    

    void Update()
    {
        var horizontal = -Input.GetAxis("Mouse Y") * sens * Time.deltaTime;
        var vertical = Input.GetAxis("Mouse X") * sens * Time.deltaTime;
        transform.Rotate(horizontal, vertical, 0);
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(new Vector3(0, 0, -1) * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(new Vector3(0, 0, 1) * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(new Vector3(0, 1, 0) * speed * Time.deltaTime);
        } 
        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(new Vector3(0, -1, 0) * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(new Vector3(1, 0, 0) * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(new Vector3(-1, 0, 0) * speed * Time.deltaTime);
        }      
    }

}
