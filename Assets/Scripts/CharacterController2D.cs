using GlobalTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    [Header("General Settings")]

    [SerializeField] float raycastDistance = 0.2f;
    [SerializeField] LayerMask layerMask;
    [SerializeField] float slopeAngleLimit = 45f;
    [SerializeField] float downForceAdjustment = 1.2f;

    [Header("Collision Flags")]
    [SerializeField] bool below;
    [SerializeField] bool left;
    [SerializeField] bool right;
    [SerializeField] bool above;
    [SerializeField] bool hitGroundThisFrame;
    [SerializeField] bool hitWallThisFrame;

    [Header("Collision Information")]
    [SerializeField] GroundType groundType;
    [SerializeField] WallType leftWallType;
    [SerializeField] bool leftIsRunnable;
    [SerializeField] bool leftIsJumpable;
    [SerializeField] float leftSlideModifier;
    [SerializeField] WallType rightWallType;
    [SerializeField] bool rightIsRunnable;
    [SerializeField] bool rightIsJumpable;
    [SerializeField] float rightSlideModifier;
    [SerializeField] GroundType ceilingType;
    [SerializeField] WallEffector leftWallEffector;
    [SerializeField] WallEffector rightWallEffector;
    [SerializeField] float jumpPadAmount;
    [SerializeField] float jumpPadUpperLimit;

    [Header("Air Effector Information")]
    [SerializeField] bool inAirEffector;
    [SerializeField] AirEffectorType airEffectorType;
    [SerializeField] float airEffectorSpeed;
    [SerializeField] Vector2 airEffectorDirection;

    [Header("Water Effector Information")]
    [SerializeField] bool inWater;
    [SerializeField] bool isSubmerged;


    Vector2 _moveAmount;
    Vector2 _currentPosition;
    Vector2 _lastPosition;
    Rigidbody2D _rigidbody;
    CapsuleCollider2D _capsuleCollider;
    Vector2[] _raycastPoisition = new Vector2[3];
    RaycastHit2D[] _raycastHits = new RaycastHit2D[3];
    bool _disableGroundCheck;
    Vector2 _slopeNormal;
    float _slopeAngle;
    bool _inAirLastFrame;
    bool _noSideCollisionLastFrame;
    Transform _tempMovingPlatform;
    Vector2 _movingPlatformVelocity;
    AirEffector _airEffector;

    #region properties
    public float RaycastDistance { get => raycastDistance; }
    public LayerMask LayerMask { get => layerMask; }
    public float SlopeAngleLimit { get => slopeAngleLimit; }
    public float DownForceAdjustment { get => downForceAdjustment; }
    public bool Below { get => below; }
    public bool Left { get => left; }
    public bool Right { get => right; }
    public bool Above { get => above; }
    public bool HitGroundThisFrame { get => hitGroundThisFrame; }
    public bool HitWallThisFrame { get => hitWallThisFrame; }
    public GroundType GroundType { get => groundType; }
    public WallType LeftWallType { get => leftWallType; }
    public bool LeftIsRunnable { get => leftIsRunnable; }
    public bool LeftIsJumpable { get => leftIsJumpable; }
    public float LeftSlideModifier { get => leftSlideModifier; }
    public WallType RightWallType { get => rightWallType; }
    public bool RightIsRunnable { get => rightIsRunnable; }
    public bool RightIsJumpable { get => rightIsJumpable; }
    public float RightSlideModifier { get => rightSlideModifier; }
    public GroundType CeilingType { get => ceilingType; }
    public WallEffector LeftWallEffector { get => leftWallEffector; }
    public WallEffector RightWallEffector { get => rightWallEffector; }
    public float JumpPadAmount { get => jumpPadAmount; }
    public float JumpPadUpperLimit { get => jumpPadUpperLimit; }
    public bool InAirEffector { get => inAirEffector; }
    public AirEffectorType AirEffectorType { get => airEffectorType; }
    public float AirEffectorSpeed { get => airEffectorSpeed; }
    public Vector2 AirEffectorDirection { get => airEffectorDirection; }
    public bool InWater { get => inWater; }
    public bool IsSubmerged { get => isSubmerged; }
    #endregion

    void Start()
    {
        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        _inAirLastFrame = !below;

        _noSideCollisionLastFrame = (!right && !left);

        _lastPosition = _rigidbody.position;

        //slope adjustment
        if (_slopeAngle != 0 && below == true)
        {
            if ((_moveAmount.x > 0f && _slopeAngle > 0f) || (_moveAmount.x < 0f && _slopeAngle < 0f))
            {
                _moveAmount.y = -Mathf.Abs(Mathf.Tan(_slopeAngle * Mathf.Deg2Rad) * _moveAmount.x);
                _moveAmount.y *= downForceAdjustment;
            }
        }

        //moving platform adjustment
        if (groundType == GroundType.MovingPlatform)
        {
            //offset the player's movement on the X with moving platform velocity
            _moveAmount.x += MovingPlatformAdjust().x;

            //if platform is moving down
            if (MovingPlatformAdjust().y < 0f)
            {
                _moveAmount.y += MovingPlatformAdjust().y;
                _moveAmount.y *= downForceAdjustment;
            }

        }

        if (groundType == GroundType.CollapsablePlatform)
        {
            if (MovingPlatformAdjust().y < 0f)
            {
                _moveAmount.y += MovingPlatformAdjust().y;
                _moveAmount.y *= downForceAdjustment * 4;
            }
        }

        //tractor beam adjustment
        if (_airEffector && airEffectorType == AirEffectorType.TractorBeam)
        {
            Vector2 airEffectorVector = airEffectorDirection * airEffectorSpeed;
            _moveAmount = Vector2.Lerp(_moveAmount, airEffectorVector, Time.deltaTime);
        }

        if (!inWater)
        {
            _currentPosition = _lastPosition + _moveAmount;
            _rigidbody.MovePosition(_currentPosition);
        }
        else
        {
            if (_rigidbody.velocity.magnitude < 10f)
            {
                _rigidbody.AddForce(_moveAmount * 300f);
            }
        }

        _moveAmount = Vector2.zero;

        if (!_disableGroundCheck)
        {
            CheckGrounded();
        }

        CheckOtherCollisions();

        if (below && _inAirLastFrame)
        {
            hitGroundThisFrame = true;
        }
        else
        {
            hitGroundThisFrame = false;
        }

        if ((right || left) && _noSideCollisionLastFrame)
        {
            hitWallThisFrame = true;
        }
        else
        {
            hitWallThisFrame = false;
        }
    }

    public void Move(Vector2 movement)
    {
        _moveAmount += movement;
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, _capsuleCollider.size, CapsuleDirection2D.Vertical, 0f, Vector2.down, raycastDistance, layerMask);

        if (hit.collider)
        {
            groundType = DetermineGroundType(hit.collider);

            _slopeNormal = hit.normal;
            _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);

            if (_slopeAngle > slopeAngleLimit || _slopeAngle < -slopeAngleLimit)
            {
                below = false;
            }
            else
            {
                below = true;
            }

            if (groundType == GroundType.JumpPad)
            {
                JumpPad jumpPad = hit.collider.GetComponent<JumpPad>();
                jumpPadAmount = jumpPad.jumpPadAmount;
                jumpPadUpperLimit = jumpPad.jumpPadUpperLimit;
            }
        }
        else
        {
            groundType = GroundType.None;
            below = false;
            if (_tempMovingPlatform)
            {
                _tempMovingPlatform = null;
            }
        }
    }

    private void CheckOtherCollisions()
    {
        //check left
        RaycastHit2D leftHit = Physics2D.BoxCast(_capsuleCollider.bounds.center, _capsuleCollider.size * 0.75f, 0f, Vector2.left, raycastDistance * 2, layerMask);

        if (leftHit.collider)
        {
            leftWallType = DetermineWallType(leftHit.collider);
            left = true;
            leftWallEffector = leftHit.collider.GetComponent<WallEffector>();

            if (leftWallEffector)
            {
                leftIsRunnable = leftWallEffector.isRunnable;
                leftIsJumpable = leftWallEffector.isJumpable;
                leftSlideModifier = leftWallEffector.wallSlideAmount;
            }
        }
        else
        {
            leftWallType = WallType.None;
            left = false;
        }

        //check right
        RaycastHit2D rightHit = Physics2D.BoxCast(_capsuleCollider.bounds.center, _capsuleCollider.size * 0.75f, 0f, Vector2.right, raycastDistance * 2, layerMask);

        if (rightHit.collider)
        {
            rightWallType = DetermineWallType(rightHit.collider);
            right = true;
            rightWallEffector = rightHit.collider.GetComponent<WallEffector>();

            if (rightWallEffector)
            {
                rightIsRunnable = rightWallEffector.isRunnable;
                rightIsJumpable = rightWallEffector.isJumpable;
                rightSlideModifier = rightWallEffector.wallSlideAmount;
            }
        }
        else
        {
            rightWallType = WallType.None;
            right = false;
        }

        //check above
        RaycastHit2D aboveHit = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, _capsuleCollider.size, CapsuleDirection2D.Vertical, 0f, Vector2.up, raycastDistance, layerMask);

        if (aboveHit.collider)
        {
            ceilingType = DetermineGroundType(aboveHit.collider);
            above = true;
        }
        else
        {
            ceilingType = GroundType.None;
            above = false;
        }
    }

    //private void CheckGrounded()
    //{
    //    Vector2 raycastOrigin = _rigidbody.position - new Vector2(0, _capsuleCollider.size.y * 0.5f);

    //    _raycastPoisition[0] = raycastOrigin + (Vector2.left * _capsuleCollider.size.x * 0.25f + Vector2.up * 0.1f);
    //    _raycastPoisition[1] = raycastOrigin;
    //    _raycastPoisition[2] = raycastOrigin + (Vector2.right * _capsuleCollider.size.x * 0.25f + Vector2.up * 0.1f);

    //    DrawDebugRays(Vector2.down, Color.green);

    //    int numberOfGroundHits = 0;

    //    for (int i = 0; i < _raycastPoisition.Length; i++)
    //    {
    //        RaycastHit2D hit = Physics2D.Raycast(_raycastPoisition[i], Vector2.down, raycastDistance, layerMask);

    //        if (hit.collider)
    //        {
    //            _raycastHits[i] = hit;
    //            numberOfGroundHits++;
    //        }
    //    }

    //    if (numberOfGroundHits > 0)
    //    {
    //        if (_raycastHits[1].collider)
    //        {
    //            groundType = DetermineGroundType(_raycastHits[1].collider);
    //            _slopeNormal = _raycastHits[1].normal;
    //            _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);
    //        }
    //        else
    //        {
    //            for (int i = 0; i < _raycastHits.Length; i++)
    //            {
    //                if (_raycastHits[i].collider)
    //                {
    //                    groundType = DetermineGroundType(_raycastHits[i].collider);
    //                    _slopeNormal = _raycastHits[i].normal;
    //                    _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);
    //                }
    //            }
    //        }

    //        if (_slopeAngle > slopeAngleLimit || _slopeAngle < -slopeAngleLimit)
    //        {
    //            below = false;
    //        }
    //        else
    //        {
    //            below = true;
    //        }
    //    }
    //    else
    //    {
    //        groundType = GroundType.None;
    //        below = false;
    //    }
    //    System.Array.Clear(_raycastHits, 0, _raycastHits.Length);
    //}

    private void DrawDebugRays(Vector2 direction, Color color)
    {
        for (int i = 0; i < _raycastPoisition.Length; i++)
        {
            Debug.DrawRay(_raycastPoisition[i], direction * raycastDistance, color);
        }
    }

    public void DisableGroundCheck()
    {
        below = false;
        _disableGroundCheck = true;
        StartCoroutine("EnableGroundCheck");
    }

    IEnumerator EnableGroundCheck()
    {
        yield return new WaitForSeconds(0.1f);
        _disableGroundCheck = false;
    }

    private GroundType DetermineGroundType(Collider2D collider)
    {
        if (collider.GetComponent<GroundEfector>())
        {
            GroundEfector groundEfector = collider.GetComponent<GroundEfector>();
            if (groundType == GroundType.MovingPlatform || groundType == GroundType.CollapsablePlatform)
            {
                if (!_tempMovingPlatform)
                {
                    _tempMovingPlatform = collider.transform;

                    if (groundType == GroundType.CollapsablePlatform)
                    {
                        _tempMovingPlatform.GetComponent<CollapsablePlatform>().CollapsePlatform();
                    }
                }
            }
            return groundEfector.groundType;
        }
        else
        {
            if (_tempMovingPlatform)
            {
                _tempMovingPlatform = null;
            }
            return GroundType.LevelGeometry;
        }
    }

    private WallType DetermineWallType(Collider2D collider)
    {
        if (collider.GetComponent<WallEffector>())
        {
            WallEffector wallEffector = collider.GetComponent<WallEffector>();
            return wallEffector.wallType;
        }
        else
        {
            return WallType.Normal;
        }
    }

    private Vector2 MovingPlatformAdjust()
    {
        if (_tempMovingPlatform && groundType == GroundType.MovingPlatform)
        {
            _movingPlatformVelocity = _tempMovingPlatform.GetComponent<MovingPlatform>().difference;
            return _movingPlatformVelocity;
        }
        else if (_tempMovingPlatform && groundType == GroundType.CollapsablePlatform)
        {
            _movingPlatformVelocity = _tempMovingPlatform.GetComponent<CollapsablePlatform>().difference;
            return _movingPlatformVelocity;
        }
        else
        {
            return Vector2.zero;
        }
    }

    public void ClearMovingPlatform()
    {
        if (_tempMovingPlatform)
        {
            _tempMovingPlatform = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<BuoyancyEffector2D>())
        {
            inWater = true;
        }

        if (collision.gameObject.GetComponent<AirEffector>())
        {
            inAirEffector = true;
            _airEffector = collision.gameObject.GetComponent<AirEffector>();

            airEffectorType = _airEffector.airEffectorType;
            airEffectorSpeed = _airEffector.speed;
            airEffectorDirection = _airEffector.direction;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.bounds.Contains(_capsuleCollider.bounds.min) && collision.bounds.Contains(_capsuleCollider.bounds.max) && collision.gameObject.GetComponent<BuoyancyEffector2D>())
        {
            isSubmerged = true;
        }
        else
        {
            isSubmerged = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<BuoyancyEffector2D>())
        {
            _rigidbody.velocity = Vector2.zero;
            inWater = false;
        }

        if (collision.gameObject.GetComponent<AirEffector>())
        {
            inAirEffector = false;
            _airEffector.DeactivateEffector();
            _airEffector = null;
            airEffectorType = AirEffectorType.None;
            airEffectorSpeed = 0f;
            airEffectorDirection = Vector2.zero;
        }
    }

    public void DeactivateAirEffector()
    {
        if (_airEffector)
        {
            _airEffector.DeactivateEffector();
        }
    }
}
