using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 5f;
    public Vector2 difference;

    private Vector3 _lastPosition;
    private Vector3 _currentWaypoint;
    private int _waypointCounter;

    void Start()
    {
        _waypointCounter = 0;
        _currentWaypoint = waypoints[_waypointCounter].position;
    }

    void Update()
    {
        _lastPosition = transform.position;

        transform.position = Vector3.MoveTowards(transform.position, _currentWaypoint, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _currentWaypoint) < 0.1f)
        {
            _waypointCounter++;
            if (_waypointCounter >= waypoints.Length)
            {
                _waypointCounter = 0;
            }
            _currentWaypoint = waypoints[_waypointCounter].position;
        }

        difference = transform.position - _lastPosition;
    }
}
