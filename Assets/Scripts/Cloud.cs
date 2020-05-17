using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    private RectTransform parent;
    private float min = 2f;
    private float max = 3f;
    [SerializeField] float Max_speed;
    [SerializeField] float Min_speed;
    private float speed = 3f;
    // Start is called before the first frame update
    void Start()
    {
        parent = GetComponentInParent<RectTransform>();

        min = parent.offsetMin.x;
        max = parent.offsetMax.x;
        speed = Random.Range(Min_speed, Max_speed);

        transform.position = new Vector3(Random.Range(min, max), Random.Range(parent.offsetMin.y, parent.offsetMax.y), 30);


    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.x > max) {
            speed = Random.Range(Min_speed, Max_speed);
            transform.position = new Vector3(min, Random.Range(parent.offsetMin.y, transform.position.z), transform.position.z);
        } else {
            transform.position = new Vector3(transform.position.x + speed * Time.deltaTime, transform.position.y, transform.position.z);
        }
        
    }
}
