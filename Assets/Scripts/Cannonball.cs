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

    void OnTriggerEnter(Collider other) {
        RaycastHit hitInfo;
        bool hit = other.Raycast(new Ray(transform.position - rigidbody.velocity, rigidbody.velocity), out hitInfo, rigidbody.velocity.magnitude * 2);
        if(hit) {
            Instantiate(explodeParticles, hitInfo.point, Quaternion.LookRotation(hitInfo.normal, Vector3.up));
        }
        Destroy(gameObject);
    }
}
