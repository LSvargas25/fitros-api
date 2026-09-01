using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CreateWorkoutRoutineResponse
{
    public CreateWorkoutRoutineResponse(Guid id, string name, int version)
    {
        Id = id;
        Name = name;
        Version = version;
    }

    public Guid Id { get; }
    public string Name { get; }
    public int Version { get; }
}
