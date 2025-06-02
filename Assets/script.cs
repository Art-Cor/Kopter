using UnityEngine;
public class script : MonoBehaviour
{
  public GameObject RotorLW, RotorRW, RotorLB, RotorRB;
  public MAVlinkResever MAVlinkResever;
  public float speed = 100;
  private bool flag = true;
  void Update()
  {
    //Debug.Log($" {MAVlinkResever.motorRpm[0]} {MAVlinkResever.motorRpm[1]} {MAVlinkResever.motorRpm[2]} {MAVlinkResever.motorRpm[3]}" );
    RotorLW.transform.Rotate(Vector3.up, speed * Time.deltaTime * MAVlinkResever.motorRpm[0]);
    RotorRW.transform.Rotate(Vector3.up, speed * Time.deltaTime * MAVlinkResever.motorRpm[1]);
    RotorLB.transform.Rotate(Vector3.up, speed * Time.deltaTime * MAVlinkResever.motorRpm[2]);
    RotorRB.transform.Rotate(Vector3.up, speed * Time.deltaTime * MAVlinkResever.motorRpm[3]);
    transform.rotation = Quaternion.Euler(
      -MAVlinkResever.pitch * Mathf.Rad2Deg,
      MAVlinkResever.yaw * Mathf.Rad2Deg,
      -MAVlinkResever.roll * Mathf.Rad2Deg
    );
    if (flag)
    {
      transform.position = MAVlinkResever.posxyz;
      flag = false;
    }
    transform.Translate(MAVlinkResever.velocity * Time.deltaTime, Space.World);
  }
  void Start()
    {
      MAVlinkResever.OnPosUpdated += RePos;
    }
  private void RePos(Vector3 pos)
  {
    flag = true;
  }
  
}

