using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;

public class ConnectionMenu : MonoBehaviour
{
    private List<IPAddress> iPAddresses = new();

    private void OnEnable()
    {
        StartCoroutine(ResearchAllAdresses());
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator ResearchAllAdresses()
    {
        while (true)
        {
            iPAddresses.Clear();

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();

                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    iPAddresses.Add(addr.Address);

                    yield return null;
                }  

                yield return null;
            }

            Debug.Log(string.Join("\n", iPAddresses));

            yield return new WaitForSecondsRealtime(2);
        }
    }   
}
