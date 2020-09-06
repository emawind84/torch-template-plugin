using NLog;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;

namespace TorchPlugin
{

    public class TestCommand : CommandModule
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Command("showorbit", "Draw the orbit using GPS waypoints")]
        [Permission(MyPromoteLevel.Admin)]
        public void ShowOrbit(string orbitCenterPositionGPSCoords, double orbitZYangle, double orbitRadius)
        {
            Vector3D orbitCenterPosition = new Vector3D(0, 0, 0);
            if (MyWaypointInfo.TryParse(orbitCenterPositionGPSCoords, out MyWaypointInfo waypointInfo))
            {
                orbitCenterPosition = waypointInfo.Coords;
            }

            int numberOfGps = 20;
            double entityXYAngle = 0;
            for (var i = 0; i < numberOfGps; i++)
            {
                var z = 0;
                var x = orbitRadius * Math.Cos(entityXYAngle);
                var y = orbitRadius * Math.Sin(entityXYAngle);

                MatrixD xRotationMatrix = new MatrixD(1, 0, 0, 0, Math.Cos(orbitZYangle), -Math.Sin(orbitZYangle), 0, Math.Sin(orbitZYangle), Math.Cos(orbitZYangle));
                
                var gpsCoords = new Vector3D(x, y, z);
                gpsCoords = Vector3D.Rotate(gpsCoords, xRotationMatrix);
                gpsCoords = Vector3D.Add(gpsCoords, orbitCenterPosition);

                MyGps gps = new MyGps
                {
                    Coords = gpsCoords,
                    Name = string.Format("WP:{0:#.###}", entityXYAngle),
                    AlwaysVisible = true,
                    ShowOnHud = true,
                    GPSColor = Color.Red
                };
                Context.Torch.CurrentSession.KeenSession.Gpss.SendAddGps(Context.Player.IdentityId, ref gps);

                Log.Info(gps);
                entityXYAngle += Math.PI * 2 / numberOfGps;
            }
        }

        [Command("orbitangle", "Calculate the orbit angle based on current location")]
        [Permission(MyPromoteLevel.Admin)]
        public void CheckYZAngle(string orbitCenterPositionGPSCoords)
        {
            if (MyWaypointInfo.TryParse(orbitCenterPositionGPSCoords, out MyWaypointInfo waypointInfo))
            {
                double angle = CalculateYZAngle(waypointInfo.Coords);
                Context.Respond("Calculated Angle RAD: " + angle + " | DEG: " + MathHelper.ToDegrees(angle));
            }
        }

        private double CalculateYZAngle(Vector3D orbitCenterPosition)
        {
            var yDirectionalVector = new Vector3D(0, 1, 0);
            var entityToPlanetDirectionalVector = Vector3D.Normalize(Context.Player.GetPosition() - orbitCenterPosition);
            var entityAngle = Math.Atan2(entityToPlanetDirectionalVector.Y, entityToPlanetDirectionalVector.Z)
                - Math.Atan2(yDirectionalVector.Y, yDirectionalVector.Z);
            if (entityAngle < 0)
            {
                entityAngle += 2 * Math.PI;
            }
            if (entityAngle > (Math.PI / 2) && entityAngle < (Math.PI * 3 / 2))
            {
                entityAngle += Math.PI;
            }
            return entityAngle;
        }

        [Command("nextwaypoint", "Find the next waypoint based on current position")]
        [Permission(MyPromoteLevel.Admin)]
        public void NextWaypoint(string orbitCenterPositionGPSCoords, double orbitZYangle, double orbitRadius = 1000)
        {
            Vector3D orbitCenterPosition = new Vector3D(0, 0, 0);
            if (MyWaypointInfo.TryParse(orbitCenterPositionGPSCoords, out MyWaypointInfo waypointInfo))
            {
                orbitCenterPosition = waypointInfo.Coords;
            }

            var xDirectionalVector = new Vector3D(1, 0, 0);
            var yDirectionalVector = new Vector3D(0, 1, 0);
            var entityToPlanetDirectionalVector = Vector3D.Normalize(Context.Player.GetPosition() - orbitCenterPosition);

            var dotX = Vector3D.Dot(xDirectionalVector, entityToPlanetDirectionalVector);
            var dotY = Vector3D.Dot(yDirectionalVector, entityToPlanetDirectionalVector);

            var entityXYAngle = Math.Acos(MathHelper.Clamp(dotX, -1f, 1f));
            if (dotY < 0)
            {
                entityXYAngle = 2 * Math.PI - entityXYAngle;
            }

            entityXYAngle += Math.PI * 2 / 20;  // next waypoint increment
            
            var z = 0;
            var x = orbitRadius * Math.Cos(entityXYAngle);
            var y = orbitRadius * Math.Sin(entityXYAngle);
            
            MatrixD xRotationMatrix = new MatrixD(1, 0, 0, 0, Math.Cos(orbitZYangle), -Math.Sin(orbitZYangle), 0, Math.Sin(orbitZYangle), Math.Cos(orbitZYangle));
            var gpsCoords = new Vector3D(x, y, z);
            gpsCoords = Vector3D.Rotate(gpsCoords, xRotationMatrix);
            gpsCoords = Vector3D.Add(gpsCoords, orbitCenterPosition);

            MyGps gps = new MyGps
            {
                Coords = gpsCoords,
                Name = string.Format("WP:{0:#.###}", entityXYAngle),
                AlwaysVisible = true,
                ShowOnHud = true,
                GPSColor = Color.Green
            };
            Context.Torch.CurrentSession.KeenSession.Gpss.SendAddGps(Context.Player.IdentityId, ref gps);
            Context.Respond($"Next waypoint: {gps}");

            Log.Info(gps);
        }
    }
}
