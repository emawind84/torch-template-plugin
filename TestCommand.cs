using NLog;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
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

        [Command("test", "This is a Test Command.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Test()
        {
            Context.Respond("This is a Test from " + Context.Player);
            //MyAPIGateway.Entities.GetEntity(entity => entity.)
            Vector3D planetCenterPosition = new Vector3D(0, 0, 0);
            double planetRadius = 1000;

            double radAngle = 0;
            for (var i = 0; i < 8; i++)
            {
                var z = 0;
                var x = planetRadius * Math.Cos(radAngle);
                var y = planetRadius * Math.Sin(radAngle);

                double entityAngle = CalculateYZAngle();
                MatrixD xRotationMatrix = new MatrixD(1, 0, 0, 0, Math.Cos(entityAngle), -Math.Sin(entityAngle), 0, Math.Sin(entityAngle), Math.Cos(entityAngle));
                
                var gpsCoords = new Vector3D(x, y, z);
                gpsCoords = Vector3D.Rotate(gpsCoords, xRotationMatrix);
                gpsCoords = Vector3D.Add(gpsCoords, planetCenterPosition);

                MyGps gps = new MyGps
                {
                    Coords = gpsCoords,
                    Name = "@" + MathHelper.ToDegrees(radAngle),
                    AlwaysVisible = true,
                    ShowOnHud = true,
                    GPSColor = Color.Red
                };
                Context.Torch.CurrentSession.KeenSession.Gpss.SendAddGps(Context.Player.IdentityId, ref gps);

                Log.Info(gps);
                radAngle += (Math.PI * 2) / 8;
            }
        }

        [Command("angle", "This is a Test Command.")]
        [Permission(MyPromoteLevel.Admin)]
        public void CheckYZAngle()
        {
            CalculateYZAngle();
        }

        private double CalculateYZAngle()
        {
            Vector3D planetCenterPosition = new Vector3D(0, 0, 0);
            var yDirectionalVector = new Vector3D(0, 1, 0);
            var entityToPlanetDirectionalVector = Vector3D.Normalize(Context.Player.GetPosition() - planetCenterPosition);
            var entityAngle = Math.Atan2(entityToPlanetDirectionalVector.Y, entityToPlanetDirectionalVector.Z)
                - Math.Atan2(yDirectionalVector.Y, yDirectionalVector.Z);
            if (entityAngle < 0) entityAngle += 2 * Math.PI;
            Context.Respond("Entity angle: " + entityAngle);
            if (entityAngle > (Math.PI / 2) && entityAngle < (Math.PI * 3 / 2))
            {

                entityAngle += Math.PI;
            }

            Context.Respond("Entity angle fix: " + entityAngle);

            return entityAngle;
        }

        [Command("next", "This is a Test Command.")]
        [Permission(MyPromoteLevel.Admin)]
        public void NextWaypoint()
        {
            Vector3D planetCenterPosition = new Vector3D(0, 0, 0);
            double planetRadius = 1000;

            var xDirectionalVector = new Vector3D(1, 0, 0);
            var yDirectionalVector = new Vector3D(0, 1, 0);
            var entityToPlanetDirectionalVector = Vector3D.Normalize(Context.Player.GetPosition() - planetCenterPosition);

            var dotX = Vector3D.Dot(xDirectionalVector, entityToPlanetDirectionalVector);
            var dotY = Vector3D.Dot(yDirectionalVector, entityToPlanetDirectionalVector);

            Context.Respond($"dotX: {dotX} - dotY: {dotY}");
            var entityXYAngle = Math.Acos(MathHelper.Clamp(dotX, -1f, 1f));
            if (dotY < 0)
            {
                entityXYAngle = 2 * Math.PI - entityXYAngle;
            }
            //var entityXYAngle = Math.Atan2(entityToPlanetDirectionalVector.Y, entityToPlanetDirectionalVector.X)
            //    - Math.Atan2(xDirectionalVector.Y, xDirectionalVector.X);
            //if (entityXYAngle < 0) entityXYAngle += 2 * Math.PI;

            entityXYAngle += (Math.PI * 2) / 20;  // next waypoint increment
            
            Context.Respond("@" + entityXYAngle + " - " + MathHelper.ToDegrees(entityXYAngle));

            var z = 0;
            var x = planetRadius * Math.Cos(entityXYAngle);
            var y = planetRadius * Math.Sin(entityXYAngle);

            double entityAngle = CalculateYZAngle();
            MatrixD xRotationMatrix = new MatrixD(1, 0, 0, 0, Math.Cos(entityAngle), -Math.Sin(entityAngle), 0, Math.Sin(entityAngle), Math.Cos(entityAngle));
            var gpsCoords = new Vector3D(x, y, z);
            gpsCoords = Vector3D.Rotate(gpsCoords, xRotationMatrix);
            gpsCoords = Vector3D.Add(gpsCoords, planetCenterPosition);

            MyGps gps = new MyGps
            {
                Coords = gpsCoords,
                Name = "@ " + MathHelper.ToDegrees(entityXYAngle),
                AlwaysVisible = true,
                ShowOnHud = true,
                GPSColor = Color.Green
            };
            Context.Torch.CurrentSession.KeenSession.Gpss.SendAddGps(Context.Player.IdentityId, ref gps);

            Log.Info(gps);
        }
    }
}
