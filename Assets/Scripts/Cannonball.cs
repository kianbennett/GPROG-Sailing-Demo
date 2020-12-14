using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannonball : MonoBehaviour {

    public float force;
    public float lifetime;
    public ParticleSystem explodeParticles;
    public new Rigidbody rigidbody;
    
    void Awake() {
        rigidbody.AddForce(transform.forward * force, ForceMode.Impulse);
        Destroy(gameObject, lifetime);
    }

    void Update() {
        // Debug.DrawLine(transform.position - transform.forward, troasftransform.forward * 2, Color.green);
        Debug.DrawRay(transform.position - rigidbody.velocity.normalized, rigidbody.velocity.normalized * 2, Color.green);
    }

    void OnTriggerEnter(Collider other) {
        RaycastHit hitInfo;
        bool hit = other.Raycast(new Ray(transform.position - rigidbody.velocity, rigidbody.velocity), out hitInfo, rigidbody.velocity.magnitude * 2);
        if(hit) {
            Instantiate(explodeParticles, hitInfo.point, Quaternion.LookRotation(hitInfo.normal, Vector3.up));
        }
        Destroy(gameObject);
    }
}
