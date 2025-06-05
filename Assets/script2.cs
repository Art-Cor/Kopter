using UnityEngine;
using System.Collections.Generic;
public class DroneCommandHandler : MonoBehaviour
{
    public MAVlinkResever MAVlinkResever;
    public GameObject waypointMarkerPrefab;
    
    private List<Vector3> missionPoints = new List<Vector3>();
    private List<GameObject> spawnedMarkers = new List<GameObject>();
    private bool land = false;

    void Update()
    {
        // Взлет
        if (Input.GetKeyDown(KeyCode.T))
        {
            MAVlinkResever.SetGuidedMode();
            MAVlinkResever.ArmDrone();
            MAVlinkResever.Takeoff();
        }
        // Посадка
        if (Input.GetKeyDown(KeyCode.L))
        {
            MAVlinkResever.DoRepos(new Vector3(0, 10, 0));
            land = true;
        }
        // Перелет к камере
        if (Input.GetKeyDown(KeyCode.C))
        {
            Vector3 cameraPos = Camera.main.transform.position;
            MAVlinkResever.DoRepos(cameraPos);
        }
        // Запомнить точку маршрута
        if (Input.GetKeyDown(KeyCode.M))
        {
            Vector3 newPoint = transform.position;
            missionPoints.Add(newPoint);
            
            // Создание точки
            if(waypointMarkerPrefab != null)
            {
                GameObject marker = Instantiate(
                    waypointMarkerPrefab,
                    newPoint,
                    Quaternion.identity
                );
                spawnedMarkers.Add(marker);
            }
        }
        // Удалить последнюю точку
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if(missionPoints.Count > 0)
            {
                missionPoints.RemoveAt(missionPoints.Count - 1);
                
                // Удалить последний маркер
                if(spawnedMarkers.Count > 0)
                {
                    GameObject lastMarker = spawnedMarkers[spawnedMarkers.Count - 1];
                    spawnedMarkers.RemoveAt(spawnedMarkers.Count - 1);
                    Destroy(lastMarker);
                }
            }
        }
        // Отправить миссию
        if (Input.GetKeyDown(KeyCode.G))
        {
            if(missionPoints.Count > 0)
            {
                // Добавляем точку возврата
                missionPoints.Add(new Vector3(0, 10, 0));
                
                MAVlinkResever.SetGuidedMode();
                MAVlinkResever.ClearMission();
                MAVlinkResever.CreateAndUploadMission(missionPoints);
            }
        }
        // Запустить миссию
        if (Input.GetKeyDown(KeyCode.B))
        {
            MAVlinkResever.SetGuidedMode();
            MAVlinkResever.ArmDrone();
            MAVlinkResever.StartMission();
        }
        // Очистить миссию
        if (Input.GetKeyDown(KeyCode.R))
        {
            missionPoints.Clear();
            MAVlinkResever.ClearMission();
            
            // Удалить все маркеры
            foreach(GameObject marker in spawnedMarkers)
            {
                Destroy(marker);
            }
            spawnedMarkers.Clear();
        }
        if ((land) && (MAVlinkResever.posxyz.x*MAVlinkResever.posxyz.x+MAVlinkResever.posxyz.z*MAVlinkResever.posxyz.z<0.0001))
        {
            MAVlinkResever.Land();
            land = false;
        }
    }
}