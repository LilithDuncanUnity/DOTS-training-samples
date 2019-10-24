﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace HighwayRacers
{
	/// <summary>
	/// Singleton class containing math functions.
	/// </summary>
    public class Highway : MonoBehaviour
    {
        public const int NUM_LANES = 8;
        public const float LANE_SPACING = 1.9f;
        public const float MID_RADIUS = 31.46f;
        public const float CURVE_LANE0_RADIUS = MID_RADIUS - LANE_SPACING * (NUM_LANES - 1) / 2f;
        public const float MIN_HIGHWAY_LANE0_LENGTH = CURVE_LANE0_RADIUS * 4;
        public const float MIN_DIST_BETWEEN_CARS = .7f;

        [Header("Prefabs")]
        public GameObject straightPiecePrefab;
        public GameObject curvePiecePrefab;

        public Mesh EntityCarMesh;
        public Material EntityCarMaterial;

        public static Highway instance { get; private set; }

        private HighwayPiece[] pieces = new HighwayPiece[8];

		/// <summary>
		/// Use this to access geometry from jobs
		/// </summary>
        public DotsHighway DotsHighway = new DotsHighway();

        public void CreateHighway(float lane0Length)
        {
            if (lane0Length < MIN_HIGHWAY_LANE0_LENGTH)
            {
                Debug.LogError("Highway length must be longer than " + MIN_HIGHWAY_LANE0_LENGTH);
                return;
            }

			int tempNumCars = NumCars;
			if (lane0Length < DotsHighway.Lane0Length)
				ClearCars();

            float straightPieceLength = (lane0Length - CURVE_LANE0_RADIUS * 4) / 4;

            Vector3 pos = Vector3.zero;
            float rot = 0;

            for (int i = 0; i < 8; i++)
            {
                if (i % 2 == 0)
                {
                    // straight piece
                    if (pieces[i] == null)
                    {
                        pieces[i] = Instantiate(straightPiecePrefab, transform).GetComponent<StraightPiece>();
                    }
                    StraightPiece straightPiece = pieces[i] as StraightPiece;
                    straightPiece.SetStartPosition(pos);
                    straightPiece.startRotation = rot;
                    straightPiece.SetLength(straightPieceLength);

                    pos += straightPiece.startRotationQ * new Vector3(0, 0, straightPieceLength);
                }
                else
                {
                    // curve piece
                    if (pieces[i] == null)
                    {
                        pieces[i] = Instantiate(curvePiecePrefab, transform).GetComponent<CurvePiece>();
                    }
                    CurvePiece curvePiece = pieces[i] as CurvePiece;
                    curvePiece.SetStartPosition(pos);
                    curvePiece.startRotation = rot;

                    pos += curvePiece.startRotationQ * new Vector3(MID_RADIUS, 0, MID_RADIUS);
                    rot = Mathf.PI / 2 * (i / 2 + 1);
                }
            }
            DotsHighway.Create(pieces);
			SetNumCars(tempNumCars);
        }

		public int NumCars { get; private set; }

		public void SetNumCars(int numCars)
        {
            var em = World.Active?.EntityManager;
            if (em == null)
                return;
            var query = em.CreateEntityQuery(typeof(CarState));
            var numExistingCars = query.CalculateEntityCount();
            int delta = numCars - numExistingCars;
            if (delta > 0)
                AddCarEntities(delta, em);
            else if (delta < 0)
            {
                var entities = query.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < -delta; ++i)
                    em.DestroyEntity(entities[i]);
                entities.Dispose();
            }
            NumCars = numCars;
        }

        public void ClearCars()
        {
            SetNumCars(0);
            NumCars = 0;
        }

        static int NextCarId = 1;
        void AddCarEntities(int count, EntityManager em)
        {
            float lane = 0;
            for (int i = 0; i < count; i++)
            {
                var entity = World.Active.EntityManager.CreateEntity();

                em.AddComponentData(entity,new CarID { Value = NextCarId++ });
                var data = new CarSettings()
                {
                    DefaultSpeed = UnityEngine.Random.Range(Game.instance.defaultSpeedMin, Game.instance.defaultSpeedMax),
                    OvertakePercent = UnityEngine.Random.Range(Game.instance.overtakePercentMin, Game.instance.overtakePercentMax),
                    LeftMergeDistance = UnityEngine.Random.Range(Game.instance.leftMergeDistanceMin, Game.instance.leftMergeDistanceMax),
                    MergeSpace = UnityEngine.Random.Range(Game.instance.mergeSpaceMin, Game.instance.mergeSpaceMax),
                    OvertakeEagerness = UnityEngine.Random.Range(Game.instance.overtakeEagernessMin, Game.instance.overtakeEagernessMax),
                };
                em.AddComponentData(entity,data);

                em.AddComponentData(entity,new CarState
                {
                    TargetFwdSpeed = data.DefaultSpeed,
                    FwdSpeed = data.DefaultSpeed,
                    LeftSpeed = 0,

                    PositionOnTrack = UnityEngine.Random.Range(0, DotsHighway.LaneLength(lane)),
                    Lane = lane,
                    TargetLane = 0,
                    CurrentState = CarState.State.NORMAL

                });
                em.AddComponentData(entity,new ColorComponent());
                em.AddComponentData(entity,new ProximityData());
                em.AddComponentData(entity,new Translation());
                em.AddComponentData(entity,new Rotation());
                em.AddComponentData(entity, new LocalToWorld());

                lane += 1;
                if (lane == NUM_LANES)
                    lane = 0;
            }
        }

		public Entity GetCarAtScreenPosition(Vector3 screenPosition, float radius)
        {
            var result = Entity.Null;
            var em = World.Active?.EntityManager;
            if (em != null)
            {
                var query = em.CreateEntityQuery(typeof(CarState));
                var entities = query.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < entities.Length; ++i)
                {
                    var state = em.GetComponentData<CarState>(entities[i]);
                    DotsHighway.GetWorldPosition(
                        state.PositionOnTrack, state.Lane, out float3 pos, out quaternion rot);
                    Vector3 carScreenPos = Camera.main.WorldToScreenPoint(new Vector3(pos.x, pos.y, pos.z));
                    carScreenPos.z = 0;
				    if (Vector3.Distance (screenPosition, carScreenPos) <= radius)
                    {
					    result = entities[i];
                        break;
                    }
                }
                entities.Dispose();
            }
            return result;
        }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
			CreateHighway(250);
        }

        private void Update()
        {
        }

        private void OnDestroy()
        {
            ClearCars();
            if (instance == this)
            {
                DotsHighway.Dispose();
                instance = null;
            }
        }
    }
}
