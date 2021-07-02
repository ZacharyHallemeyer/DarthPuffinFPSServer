using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentGenerator : MonoBehaviour
{
    public static Dictionary<int, GameObject> planets = new Dictionary<int, GameObject>();

    public int minPositionX, maxPositionX, minPositionY, maxPositionY, minPositionZ, maxPositionZ;
    public int minScale, maxScale;
    public int minPlanetCount, maxPlanetCount;
    public float minPlanetDistance, maxPlanetDistance;
    private int index;

    public GameObject planetBase;

    private void Start()
    {
        Random.InitState(Random.Range(0, 10000));
        StartCoroutine(GeneratePlanets());
    }

    private IEnumerator GeneratePlanets()
    {
        int _errorCatcher, _maxErrorCatcher = 10000,_planetCount = Random.Range(minPlanetCount, maxPlanetCount);
        float _planetScale; 
        Vector3 _planetPosition;
        index = 0;

        for (int i = 0; i < _planetCount; i++)
        {
            _errorCatcher = 0;
            do
            {
                if (_errorCatcher >= _maxErrorCatcher)
                    yield return new WaitForEndOfFrame();
                _planetScale = Random.Range(minScale, maxScale);
                _planetPosition = GenerateRandomVector();
                _errorCatcher++;
            }
            while (ColliderInPosition(_planetScale, _planetPosition));

            GameObject _planet = Instantiate(planetBase, _planetPosition, Quaternion.identity);
            _planet.transform.localScale = new Vector3(_planetScale, _planetScale, _planetScale);

            planets.Add(index, _planet);
            index++;

            yield return new WaitForEndOfFrame();
        }
    }

    private bool ColliderInPosition(float _scale, Vector3 _position)
    {
        float _planetDistance = Random.Range(minPlanetDistance, maxPlanetDistance);
        return Physics.CheckSphere(_position, _scale * _planetDistance);
    }

    private Vector3 GenerateRandomVector()
    {
        return new Vector3(Random.Range(minPositionX, maxPositionX), Random.Range(minPositionY, maxPositionY), Random.Range(minPositionZ, maxPositionZ));
    }
}
