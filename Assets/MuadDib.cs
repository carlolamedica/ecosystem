using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuadDib : MonoBehaviour
{
    // STATS
    public float hunger;
    public float reproductionUrge;
    public bool hasGrown;
    public bool isBeingChased;
    public Transform chaser;

    public State currentState;
    public Transform approachingTarget;
    private readonly float hungerMultiplier = 1.25f;
    private readonly float urgeMultiplier = 0.87f;
    private readonly float timeBeforeGrowing = 10f;
    private float currentSpeed;

    // REFERENCES
    public DNA dna;
    private Rigidbody2D rb;

    [Space]
    [SerializeField] private float maxHunger;
    [SerializeField] private LayerMask wallLayer;

    private void Start()
    {
        currentSpeed = dna.speed;
        hasGrown = false;
        rb = GetComponent<Rigidbody2D>();
        currentState = State.Wandering;
        StartCoroutine(Wander());
        StartCoroutine(Grow());

        StatsManager statsManager = StatsManager.Instance;
        if (!dna.isFemale)
            statsManager.nOfMales += 1;

        statsManager.speedSum += dna.speed;
        statsManager.distanceSum += dna.sensoryDistance;
        statsManager.currentPopulation += 1;
    }

    IEnumerator Grow()
    {
        yield return new WaitForSeconds(timeBeforeGrowing);
        hasGrown = true;
        transform.localScale = new Vector3(1, 1, 1);
    }

    private void Update()
    {
        if (currentState != State.Mating || currentState != State.Eating)
            hunger += Time.deltaTime * dna.speed / hungerMultiplier; // hungerMultiplier = 1.25f

        if (hunger >= maxHunger)
            Die();

        if (hasGrown)
            reproductionUrge += (Time.deltaTime * urgeMultiplier);
    }

    // For wandering
    private Vector2 randomDirection;
    // For fleeing
    private Vector2 fleeingDirection;
    private Vector2 chaserPos;

    private void FixedUpdate()
    {
        if (isBeingChased && IsInChasersRange())
        {
            if (currentState == State.Mating)
            {
                if (dna.isFemale)
                    StopCoroutine(Mating(approachingTarget.transform));
                else
                    StopCoroutine(FatherMating());
            }
            currentState = State.Flee;
            approachingTarget = null;

            if (chaser != null)
            {
                chaserPos = new(chaser.position.x, chaser.position.y);
                fleeingDirection = -(chaserPos - rb.position);
                rb.MovePosition(rb.position + (currentSpeed * Time.fixedDeltaTime * fleeingDirection.normalized));
            }
            else
            {
                currentState = State.Wandering;
                StartCoroutine(Wander());
            }
        }
        else
        {
            isBeingChased = false;
            currentSpeed = dna.speed;
            if (currentState == State.Wandering)
            {
                rb.MovePosition(rb.position + (currentSpeed * Time.fixedDeltaTime * randomDirection.normalized));
            }
            else if (currentState == State.Approaching)
            {
                if (approachingTarget != null)
                {
                    if (approachingTarget.GetComponent<MuadDib>() != null)
                        CheckMatingSensors();

                    Vector2 direction = new Vector2(approachingTarget.position.x, approachingTarget.position.y) - rb.position;
                    rb.MovePosition(rb.position + (currentSpeed * Time.fixedDeltaTime * direction.normalized));
                }
                else // Target either died or has been eaten before arrival
                {
                    currentState = State.Wandering;
                    StartCoroutine(Wander());
                    approachingTarget = null;
                }
            }
            else if (currentState == State.Flee)
            {
                chaser = null;
                currentState = State.Wandering;
                StartCoroutine(Wander());
            }

            if (currentState != State.Mating)
                CheckSensors();
        }
    }

    private bool IsInChasersRange()
    {
        if (chaser != null)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(chaser.position, chaser.GetComponent<ShaiHulud>().dna.sensoryDistance);
            bool isInRange = false;
            foreach (Collider2D collider in colliders)
            {
                if (transform == chaser.GetComponent<ShaiHulud>().approachingTarget)
                {
                    isInRange = true;
                    break;
                }
            }

            return isInRange;
        }
        else
            return false;
    }

    private void CheckSensors()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(rb.position, dna.sensoryDistance);

        bool targetIsInRange = false;
        float minDistance = Mathf.Infinity;
        Transform closestPlant = null;
        foreach (Collider2D collider in colliders)
        {
            if (approachingTarget == collider.transform)
                targetIsInRange = true;

            float distance = Vector2.Distance(transform.position, collider.transform.position);
            if (collider.transform.CompareTag("Plant") && hunger > reproductionUrge && distance < minDistance)
            {
                minDistance = distance;
                closestPlant = collider.transform;
            }
            else if (collider.transform.CompareTag("MuadDib"))
            {
                if (approachingTarget == collider.transform)
                    targetIsInRange = true;

                if (reproductionUrge > hunger && hasGrown)
                {
                    MuadDib colliderMuadDib = collider.GetComponent<MuadDib>();
                    if (colliderMuadDib.dna.isFemale != dna.isFemale) // Opposite gender
                    {
                        if (colliderMuadDib.currentState == State.Approaching && colliderMuadDib.approachingTarget == transform) // If the other MuadDib is alreay searching but not for us, we can skip ahead
                        {
                            approachingTarget = collider.transform;
                            currentState = State.Approaching;
                            return;
                        }
                        else if (colliderMuadDib.currentState == State.Wandering && colliderMuadDib.reproductionUrge > colliderMuadDib.hunger) // If the other MuadDib is wandering but wants to reproduce
                        {
                            approachingTarget = collider.transform;
                            currentState = State.Approaching;
                            return;
                        }
                    }
                }
            }
        }

        if (closestPlant != null)
        {
            Plant plant = closestPlant.GetComponent<Plant>();
            if (plant.isBeingEaten && approachingTarget == closestPlant) // The plant is being eaten, but by us so we can keep approaching
            {
                currentState = State.Approaching;
                approachingTarget = closestPlant;

            }
            else if (!plant.isBeingEaten)
            {
                currentState = State.Approaching;
                approachingTarget = closestPlant;
                plant.isBeingEaten = true;
            }

            targetIsInRange = true;
        }

        if (approachingTarget != null && !targetIsInRange)
        {
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
            if (collider.CompareTag("MuadDib") && currentState != State.Mating)
            {
                Transform collisionTransform = collider.transform;
                MuadDib approachingMuadDib = collisionTransform.GetComponent<MuadDib>();
                if (approachingMuadDib.currentState == State.Approaching && approachingMuadDib.approachingTarget == transform) // They are both searching for each other
                {
                    currentState = State.Mating;
                    if (dna.isFemale)
                        StartCoroutine(Mating(collisionTransform));
                    else
                        StartCoroutine(FatherMating());
                }
                else if (approachingMuadDib.currentState != State.Approaching)
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


    IEnumerator Eat(Plant plant)
    {
        if (approachingTarget != null && approachingTarget.GetComponent<MuadDib>() == null)
        {
            plant.isBeingEaten = true;
            yield return new WaitForSeconds(2f);
            currentState = State.Wandering;
            StartCoroutine(Wander());
            if (approachingTarget != null)
            {
                Destroy(approachingTarget.gameObject);
                hunger = 0;
            }

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
            if (collisionTransform.CompareTag("Plant"))
            {
                currentState = State.Eating;
                StartCoroutine(Eat(collisionTransform.GetComponent<Plant>()));
            }
        }
    }

    private IEnumerator FatherMating()
    {
        yield return new WaitForSeconds(1.5f);
        if (!isBeingChased)
        {
            approachingTarget = null;
            reproductionUrge = 0;
        }
        currentState = State.Wandering;
        StartCoroutine(Wander());
    }

    private IEnumerator Mating(Transform fatherTransform)
    {
        yield return new WaitForSeconds(1.5f);
        if (chaser != null)
            isBeingChased = IsInChasersRange();
        else
            isBeingChased = false;
        if (!isBeingChased && fatherTransform != null && fatherTransform.GetComponent<MuadDib>() != null && !fatherTransform.GetComponent<MuadDib>().isBeingChased)
        {
            MuadDib fatherMuadDib = fatherTransform.GetComponent<MuadDib>();

            GameObject child = Instantiate(gameObject, Spawner.Instance.transform);
            child.transform.localScale = new(0.5f, 0.5f, 1);
            MuadDib childMuadDib = child.GetComponent<MuadDib>();
            childMuadDib.approachingTarget = null;
            childMuadDib.currentState = State.Wandering;

            DNA childsDNA = dna.CrossingOver(fatherMuadDib.dna, dna);
            DNA childRefDNA = childMuadDib.dna;


            childRefDNA.isFemale = childsDNA.isFemale;
            if (childsDNA.isFemale)
                child.GetComponent<SpriteRenderer>().color = Color.magenta;
            else
                child.GetComponent<SpriteRenderer>().color = Color.blue;

            childRefDNA.speed = childsDNA.speed;
            childRefDNA.sensoryDistance = childsDNA.sensoryDistance;

            approachingTarget = null;
            reproductionUrge = 0;

        }
        currentState = State.Wandering;
        StartCoroutine(Wander());
    }

    public void Die()
    {
        StatsManager statsManager = StatsManager.Instance;
        if (!dna.isFemale)
            statsManager.nOfMales -= 1;

        statsManager.deadPopulation += 1;
        statsManager.deathSpeedSum += dna.speed;

        statsManager.speedSum -= dna.speed;
        statsManager.distanceSum -= dna.sensoryDistance;
        statsManager.currentPopulation -= 1;

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
