using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaiHulud : MonoBehaviour
{
    // STATS
    public float hunger;
    public float reproductionUrge;
    public bool hasGrown;

    public State currentState;
    public Transform approachingTarget;
    private readonly float hungerMultiplier = 2.5f;
    private readonly float urgeMultiplier = 0.5f;
    private readonly float timeBeforeGrowing = 10f;

    // REFERENCES
    public DNA dna;
    private Rigidbody2D rb;

    [Space]
    [SerializeField] private float maxHunger;
    [SerializeField] private LayerMask wallLayer;

    private void Start()
    {
        hasGrown = false;
        rb = GetComponent<Rigidbody2D>();
        currentState = State.Wandering;
        StartCoroutine(Wander());
        StartCoroutine(Grow());

        /*
        StatsManager statsManager = StatsManager.Instance;
        if (!dna.isFemale)
            statsManager.nOfMales += 1;

        statsManager.speedSum += dna.speed;
        statsManager.distanceSum += dna.sensoryDistance;
        statsManager.currentPopulation += 1;
        */
    }

    IEnumerator Grow()
    {
        yield return new WaitForSeconds(timeBeforeGrowing);
        hasGrown = true;
        transform.localScale = new Vector3(3, 3, 1);
    }

    private void Update()
    {
        if (currentState == State.Wandering || currentState == State.Approaching)
            hunger += Time.deltaTime * dna.speed / hungerMultiplier; // Hr = v/Hm

        if (hunger >= maxHunger)
            Die();

        if (hasGrown)
            reproductionUrge += (Time.deltaTime * urgeMultiplier);
    }

    private Vector2 randomDirection; // For wandering
    private void FixedUpdate()
    {

        if (currentState == State.Wandering)
        {
            rb.MovePosition(rb.position + (dna.speed * Time.fixedDeltaTime * randomDirection.normalized));
        }
        else if (currentState == State.Approaching)
        {
            if (approachingTarget != null)
            {
                if (approachingTarget.GetComponent<ShaiHulud>() != null)
                    CheckMatingSensors();

                Vector2 direction = new Vector2(approachingTarget.position.x, approachingTarget.position.y) - rb.position;
                rb.MovePosition(rb.position + (dna.speed * Time.fixedDeltaTime * direction.normalized));
            }
            else // Target either died or has been eaten before arrival
            {
                currentState = State.Wandering;
                StartCoroutine(Wander());
                approachingTarget = null;
            }
        }

        if (currentState != State.Mating)
            CheckSensors();
    }

    private void CheckSensors()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(rb.position, dna.sensoryDistance);

        bool targetIsInRange = false;
        float minDistance = Mathf.Infinity;
        Collider2D closestMuadDib = null;

        foreach (Collider2D collider in colliders)
        {
            if (approachingTarget == collider.transform)
                targetIsInRange = true;

            float distance = Vector2.Distance(transform.position, collider.transform.position);
            if (collider.transform.CompareTag("MuadDib") && hunger > reproductionUrge && distance < minDistance)
            {
                minDistance = distance;
                closestMuadDib = collider;

            }
            else if (collider.transform.CompareTag("ShaiHulud"))
            {
                if (reproductionUrge > hunger && hasGrown)
                {
                    ShaiHulud colliderShaiHulud = collider.GetComponent<ShaiHulud>();
                    if (colliderShaiHulud.dna.isFemale != dna.isFemale) // Opposite gender
                    {
                        if (colliderShaiHulud.currentState == State.Approaching && colliderShaiHulud.approachingTarget == transform) // If the other MuadDib is alreay searching but not for us, we can skip ahead
                        {
                            approachingTarget = collider.transform;
                            currentState = State.Approaching;
                            return;
                        }
                        else if (colliderShaiHulud.currentState == State.Wandering && colliderShaiHulud.reproductionUrge > colliderShaiHulud.hunger) // If the other MuadDib is wandering but wants to reproduce
                        {
                            approachingTarget = collider.transform;
                            currentState = State.Approaching;
                            return;
                        }
                    }
                }
            }
        }

        if (closestMuadDib != null)
        {
            MuadDib colliderMuadDib = closestMuadDib.GetComponent<MuadDib>();
            if (colliderMuadDib.isBeingChased && approachingTarget == colliderMuadDib.transform) // The MuadDib is being chased, but by us so we can keep approaching
            {
                currentState = State.Approaching;
                approachingTarget = closestMuadDib.transform;

                colliderMuadDib.isBeingChased = true;
                colliderMuadDib.chaser = transform;
            }
            else if (!colliderMuadDib.isBeingChased)
            {
                currentState = State.Approaching;
                approachingTarget = closestMuadDib.transform;
                colliderMuadDib.isBeingChased = true;
                colliderMuadDib.chaser = transform;
            }

            targetIsInRange = true;
        }


        if (approachingTarget != null && !targetIsInRange)
        {
            if (approachingTarget.GetComponent<MuadDib>() != null)
            {
                MuadDib approachingMuadDib = approachingTarget.GetComponent<MuadDib>();
                approachingMuadDib.isBeingChased = false;
                approachingMuadDib.chaser = null;
                approachingMuadDib.currentState = State.Wandering;
                StartCoroutine(approachingMuadDib.Wander());
            }

            currentState = State.Wandering;
            StartCoroutine(Wander());
            approachingTarget = null;
        }
    }

    private void CheckMatingSensors()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(rb.position, 3);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("ShaiHulud") && currentState != State.Mating)
            {
                Transform collisionTransform = collider.transform;
                ShaiHulud approachingShaiHulud = collisionTransform.GetComponent<ShaiHulud>();
                if (approachingShaiHulud.currentState == State.Approaching && approachingShaiHulud.approachingTarget == transform) // They are both searching for each other
                {
                    currentState = State.Mating;
                    if (dna.isFemale)
                        StartCoroutine(Mating(collisionTransform));
                    else
                        StartCoroutine(FatherMating());
                }
                else if (approachingShaiHulud.currentState != State.Approaching)
                {
                    currentState = State.Mating;
                    if (dna.isFemale)
                        StartCoroutine(Mating(collisionTransform));
                    else
                        StartCoroutine(FatherMating());
                }
            }
        }
    }

    private bool isAlreadyWandering = false;
    public IEnumerator Wander()
    {
        if (currentState == State.Wandering && !isAlreadyWandering)
        {
            randomDirection = new(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            RaycastHit2D hit = Physics2D.Raycast(rb.position, randomDirection, 30, wallLayer);
            if (hit)
            {
                StartCoroutine(Wander());
            }
            else
            {
                isAlreadyWandering = true;
                yield return new WaitForSeconds(Random.Range(2f, 6f));
                isAlreadyWandering = false;
                StartCoroutine(Wander());
            }
        }
    }

    private void Eat()
    {
        if (approachingTarget != null && approachingTarget.GetComponent<ShaiHulud>() == null && approachingTarget.GetComponent<Plant>() == null)
        {
            currentState = State.Wandering;
            StartCoroutine(Wander());
            approachingTarget.GetComponent<MuadDib>().Die();
            hunger = 0;
            approachingTarget = null;
        }
        else
        {
            currentState = State.Wandering;
            StartCoroutine(Wander());
            approachingTarget = null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Transform collisionTransform = collision.transform;
        if (currentState == State.Approaching && approachingTarget == collisionTransform) // We have hit the target we were approaching
        {
            if (collisionTransform.CompareTag("MuadDib"))
            {
                currentState = State.Eating;
                Eat();
            }
        }
    }

    private IEnumerator FatherMating()
    {
        yield return new WaitForSeconds(1.5f);
        currentState = State.Wandering;
        approachingTarget = null;
        reproductionUrge = 0;
    }

    private IEnumerator Mating(Transform fatherTransform)
    {
        yield return new WaitForSeconds(1.5f);
        if (fatherTransform != null && fatherTransform.GetComponent<ShaiHulud>() != null)
        {
            ShaiHulud fatherMuadDib = fatherTransform.GetComponent<ShaiHulud>();

            GameObject child = Instantiate(gameObject, Spawner.Instance.transform);
            child.transform.localScale = new(1f, 1f, 1);
            ShaiHulud childMuadDib = child.GetComponent<ShaiHulud>();
            childMuadDib.approachingTarget = null;
            childMuadDib.currentState = State.Wandering;

            DNA childsDNA = dna.CrossingOver(fatherMuadDib.dna, dna);
            DNA childRefDNA = childMuadDib.dna;


            childRefDNA.isFemale = childsDNA.isFemale;
            if (childsDNA.isFemale)
                child.GetComponent<SpriteRenderer>().color = Color.white;
            else
                child.GetComponent<SpriteRenderer>().color = Color.black;

            childRefDNA.speed = childsDNA.speed;
            childRefDNA.sensoryDistance = childsDNA.sensoryDistance;

            approachingTarget = null;
            reproductionUrge = 0;

        }
        currentState = State.Wandering;
    }



    public void Die()
    {
        /*
        StatsManager statsManager = StatsManager.Instance;
        if (!dna.isFemale)
            statsManager.nOfMales -= 1;

        statsManager.deadPopulation += 1;
        statsManager.deathSpeedSum += dna.speed;

        statsManager.speedSum -= dna.speed;
        statsManager.distanceSum -= dna.sensoryDistance;
        statsManager.currentPopulation -= 1;
        */

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(rb.position, randomDirection * 30);
        Gizmos.color = new(100, 100, 100, 0.5f);
        Gizmos.DrawSphere(transform.position, dna.sensoryDistance);
    }
}
