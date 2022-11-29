using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfMSMQDems.Models
{
  [Serializable]
  public class Flight
  {
    public int Id { get; set; }
    public string FlightNo { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public override string ToString()
    {
      return $"Id: {Id}, FlightNo: {FlightNo}, Origin: {Origin}, Destination: {Destination}";
    }
  }
}
