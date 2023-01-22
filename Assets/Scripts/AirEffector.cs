using GlobalTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirEffector : MonoBehaviour
{
    public AirEffectorType airEffectorType;
    public float speed;
    public Vector2 direction;
    private BoxCollider2D _collider;

    void Start()
    {
        direction = transform.up;
        _collider = gameObject.GetComponent<BoxCollider2D>();
    }

    public void DeactivateEffector()
    {
        StartCoroutine("DeactivateEffectorCoroutine");
    }

    IEnumerator DeactivateEffectorCoroutine()
    {
        _collider.enabled = false;
        yield return new WaitForSeconds(0.5f);
        _collider.enabled = true;
    }
}
