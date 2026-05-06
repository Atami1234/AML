using System.Collections.Generic;

namespace AmlDetectionApi.DTOs
{
    public class NetworkGraphDto
    {
        public List<NetworkNodeDto> Nodes { get; set; } = new List<NetworkNodeDto>();
        public List<NetworkEdgeDto> Edges { get; set; } = new List<NetworkEdgeDto>();
    }
}
