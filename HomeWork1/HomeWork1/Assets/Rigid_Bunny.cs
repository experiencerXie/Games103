
using UnityEngine;
using System.Collections;
using UnityEngine.PlayerLoop;

public class Rigid_Bunny : MonoBehaviour 
{
	bool launched 		= false;
	float dt 			= 0.015f;
	float timeTotal = 0;
	Vector3 v 			= new Vector3(0, 0, 0);	// velocity
	Vector3 w 			= new Vector3(0, 0, 0);	// angular velocity
	
	float mass;									// mass
	Matrix4x4 I_ref;							// reference inertia

	float linear_decay	= 0.999f;				// for velocity decay
	float angular_decay	= 0.98f;				
	float restitution 	= 0.5f;					// for collision
	float restitution_T = 0.2f;
	
	Vector3 gravity =new Vector3(0.0f, -9.8f, 0.0f);

	// Use this for initialization
	void Start () 
	{		
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;

		float m=1;
		mass=0;
		for (int i=0; i<vertices.Length; i++) 
		{
			mass += m;
			float diag=m*vertices[i].sqrMagnitude;
			I_ref[0, 0]+=diag;
			I_ref[1, 1]+=diag;
			I_ref[2, 2]+=diag;
			I_ref[0, 0]-=m*vertices[i][0]*vertices[i][0];
			I_ref[0, 1]-=m*vertices[i][0]*vertices[i][1];
			I_ref[0, 2]-=m*vertices[i][0]*vertices[i][2];
			I_ref[1, 0]-=m*vertices[i][1]*vertices[i][0];
			I_ref[1, 1]-=m*vertices[i][1]*vertices[i][1];
			I_ref[1, 2]-=m*vertices[i][1]*vertices[i][2];
			I_ref[2, 0]-=m*vertices[i][2]*vertices[i][0];
			I_ref[2, 1]-=m*vertices[i][2]*vertices[i][1];
			I_ref[2, 2]-=m*vertices[i][2]*vertices[i][2];
		}
		I_ref [3, 3] = 1;
	}
	
	Matrix4x4 Get_Cross_Matrix(Vector3 a)
	{
		//Get the cross product matrix of vector a
		Matrix4x4 A = Matrix4x4.zero;
		A [0, 0] = 0; 
		A [0, 1] = -a [2]; 
		A [0, 2] = a [1]; 
		A [1, 0] = a [2]; 
		A [1, 1] = 0; 
		A [1, 2] = -a [0]; 
		A [2, 0] = -a [1]; 
		A [2, 1] = a [0]; 
		A [2, 2] = 0; 
		A [3, 3] = 1;
		return A;
	}

	// In this function, update v and w by the impulse due to the collision with
	//a plane <P, N>
	void Collision_Impulse(Vector3 P, Vector3 N)
	{
		var R = Matrix4x4.Rotate(transform.rotation);
		Vector3 pos = transform.position;
		
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;
		for (int i=0; i<vertices.Length; i++)
		{
			var Rri = R * vertices[i];
			var vecticePos = pos + (Vector3)Rri;
			
			//check the vertice is in plane
			if (Vector3.Dot(vecticePos - P, N) >= 0)
				continue;
			
			//check the velocity is directed in plane
			var tmpV = v + Vector3.Cross(w, Rri);
			if (Vector3.Dot(tmpV, N) >= 0)
				continue;
			
			//calculate vertice's new velocity
			var vin = Vector3.Dot(tmpV, N) * N;
			var vit = tmpV - vin;
			var vinNew = -restitution * vin;
			var vitNew = Mathf.Max(1 - restitution_T * (1 + restitution) * Mathf.Abs(vin.magnitude) / Mathf.Abs(vit.magnitude), 0) * vit;
			var viNew = vinNew + vitNew;
			
			//calculate Impulse
			var I = R * I_ref * R.transpose;
			var Rris = Get_Cross_Matrix(Rri);
			var kTmp = Rris * I.inverse * Rris;
			Matrix4x4 k = Matrix4x4.zero;
			k [0, 0] = 1 / mass - kTmp[0, 0]; 
			k [0, 1] = - kTmp[0, 1]; 
			k [0, 2] = - kTmp[0, 2]; 
			k [0, 3] = - kTmp[0, 3]; 
			
			k [1, 0] = - kTmp[1, 0]; 
			k [1, 1] = 1 / mass - kTmp[1, 1]; 
			k [1, 2] = - kTmp[1, 2]; 
			k [1, 3] = - kTmp[1, 3]; 
			
			k [2, 0] = - kTmp[2, 0]; 
			k [2, 1] = - kTmp[2, 1]; 
			k [2, 2] = 1 / mass - kTmp[2, 2]; 
			k [2, 3] = - kTmp[2, 3]; 
			
			k [3, 0] = - kTmp[3, 0];
			k [3, 1] = - kTmp[3, 1];
			k [3, 2] = - kTmp[3, 2];
			k [3, 3] = 1 / mass - kTmp[3, 3];
			
			var j = k.inverse * (viNew - tmpV);

			Vector3 vtmp = (1 / mass * j);
			v = v + vtmp;
			Vector3 wtmp = I.inverse * (Rris * j);
			w = w + wtmp;
			break;
		}
	}

	// Update is called once per frame
	void Update () 
	{
		//Game Control
		if(Input.GetKey("r"))
		{
			transform.position = new Vector3 (0, 0.6f, 0);
			restitution = 0.5f;
			launched=false;
		}
		if(Input.GetKey("l"))
		{
			v = new Vector3 (5, 2, 0);
			w = new Vector3(0, 1, 0);
			launched=true;
		}

		if (launched == false)
			return;

		// timeTotal += Time.deltaTime;
		//
		// if (timeTotal / dt <= 0)
		// 	return;

		int updateTimes = 1;//Mathf.FloorToInt(timeTotal / dt);
		timeTotal = timeTotal % dt;

		for (int i = 0; i < updateTimes; i++)
		{
			// Part I: Update velocities
			v = v + dt * gravity;
			v = linear_decay * v;
			w = angular_decay * w;

			// Part II: Collision Impulse
			Collision_Impulse(new Vector3(0, 0.01f, 0), new Vector3(0, 1, 0));
			Collision_Impulse(new Vector3(2, 0, 0), new Vector3(-1, 0, 0));

			// Part III: Update position & orientation
			//Update linear status
			Vector3 x    = transform.position;
			x = x + v * dt;
			//Update angular status
			Quaternion q = transform.rotation;
			
			Vector3 wt = dt * w;
			Quaternion dq = new Quaternion(wt.x, wt.y, wt.z, 0.0f) * q;
			q.Set(q.x + dq.x, q.y + dq.y, q.z + dq.z, q.w + dq.w);

			// Part IV: Assign to the object
			transform.position = x;
			transform.rotation = q;
		}
	}
}
