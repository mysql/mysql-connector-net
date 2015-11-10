// Copyright © 2013, 2014, Oracle and/or its affiliates. All rights reserved.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.Types;
using MySql.Data.MySqlClient;
#if EF6
using System.Data.Entity.Spatial;
#else
using System.Data.Spatial;
#endif


namespace MySql.Data.Entity
{
  internal sealed class MySqlSpatialServices : DbSpatialServices
  {
    
    internal static readonly MySqlSpatialServices Instance = new MySqlSpatialServices();

    private MySqlSpatialServices()
      {
      }

    #region overriden methods

    public override byte[] AsBinary(DbGeography geographyValue)
    {
      throw new NotImplementedException();
    }

    public override byte[] AsBinary(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var providerValue = new MySqlGeometry();
      MySqlGeometry.TryParse(geometryValue.ProviderValue.ToString(), out providerValue);

      return providerValue.Value;
    }

    public override string AsGml(DbGeometry geometryValue)
    {
      throw new NotImplementedException();
    }

    public override string AsGml(DbGeography geographyValue)
    {
       throw new NotImplementedException();
    }

    public override string AsText(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var providerValue = new MySqlGeometry();
      MySqlGeometry.TryParse(geometryValue.ProviderValue.ToString(), out providerValue);

      return providerValue.ToString();
    }

    public override string AsText(DbGeography geographyValue)
    {
       throw new NotImplementedException();
    }

    public override DbGeometry Buffer(DbGeometry geometryValue, double distance)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      DbGeometry mysqlValue = DbGeometry.FromText(geometryValue.ProviderValue.ToString());
      return mysqlValue.Buffer(distance);     
    }

    public override DbGeography Buffer(DbGeography geographyValue, double distance)
    {
      throw new NotImplementedException();        
    }

     public override bool Contains(DbGeometry geometryValue, DbGeometry otherGeometry)
     {
       if (geometryValue == null)
         throw new ArgumentNullException("geometryValue");

       if (otherGeometry == null)
         throw new ArgumentNullException("otherGeometry");

       DbGeometry mysqlValue = DbGeometry.FromText(geometryValue.ProviderValue.ToString());
       return mysqlValue.Contains(otherGeometry);

     }

     public override object CreateProviderValue(DbGeometryWellKnownValue wellKnownValue)
     {
       if (wellKnownValue == null)
          throw new ArgumentNullException("wellKnownValue");

       if (wellKnownValue.WellKnownText != null)
       {
         var mysqlGeometry = new MySqlGeometry(true);
         MySqlGeometry.TryParse(wellKnownValue.WellKnownText.ToString(), out mysqlGeometry);
         return mysqlGeometry;          
       }
       else if (wellKnownValue.WellKnownBinary != null)
       {
         var mysqlGeometry = new MySqlGeometry(MySqlDbType.Geometry, wellKnownValue.WellKnownBinary);         
         return mysqlGeometry;                 
       }
       return null;
      }

    public override object CreateProviderValue(DbGeographyWellKnownValue wellKnownValue)
    {
        throw new NotImplementedException();
    }

    public override DbGeometryWellKnownValue CreateWellKnownValue(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");    

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.WellKnownValue;
    
    }

    public override DbGeographyWellKnownValue CreateWellKnownValue(DbGeography geographyValue)
    {
          throw new NotImplementedException();
    }

    public override bool Crosses(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);
      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Crosses(mysqlOtherValue);
    }

    public override DbGeometry Difference(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);
      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Difference(mysqlOtherValue);

    }

    public override DbGeography Difference(DbGeography geographyValue, DbGeography otherGeography)
    {
        throw new System.NotImplementedException();
    }

    public override bool Disjoint(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
        throw new System.NotImplementedException();
    }

    public override bool Disjoint(DbGeography geographyValue, DbGeography otherGeography)
    {
        throw new System.NotImplementedException();
    }

    public override double Distance(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");
         
      DbGeometry mysqlValue = DbGeometry.FromText(geometryValue.ProviderValue.ToString());

      Double? result = mysqlValue.Distance(otherGeometry);
      if (result != null)
        return result.Value;
      return 0;
    }

    public override double Distance(DbGeography geographyValue, DbGeography otherGeography)
    {
       throw new NotImplementedException();
    }

    public override DbGeometry ElementAt(DbGeometry geometryValue, int index)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.ElementAt(index);
    }

    public override DbGeography ElementAt(DbGeography geographyValue, int index)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyCollectionFromBinary(byte[] geographyCollectionWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyCollectionFromText(string geographyCollectionWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyFromBinary(byte[] wellKnownBinary, int coordinateSystemId)
    {
          throw new NotImplementedException();
    }

    public override DbGeography GeographyFromBinary(byte[] wellKnownBinary)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyFromGml(string geographyMarkup, int coordinateSystemId)
    {
          throw new NotImplementedException();
    }

    public override DbGeography GeographyFromGml(string geographyMarkup)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyFromProviderValue(object providerValue)
    {
          throw new NotImplementedException();
    }

    public override DbGeography GeographyFromText(string wellKnownText, int coordinateSystemId)
    {
          throw new NotImplementedException();
    }

    public override DbGeography GeographyFromText(string wellKnownText)
    {
          throw new NotImplementedException();
    }

    public override DbGeography GeographyLineFromBinary(byte[] lineWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyLineFromText(string lineWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyMultiLineFromBinary(byte[] multiLineWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyMultiLineFromText(string multiLineWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyMultiPointFromBinary(byte[] multiPointWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyMultiPointFromText(string multiPointWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyMultiPolygonFromBinary(byte[] multiPolygonWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyMultiPolygonFromText(string multiPolygonWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyPointFromBinary(byte[] pointWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyPointFromText(string pointWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyPolygonFromBinary(byte[] polygonWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeography GeographyPolygonFromText(string polygonWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryCollectionFromBinary(byte[] geometryCollectionWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryCollectionFromText(string geometryCollectionWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryFromBinary(byte[] wellKnownBinary, int coordinateSystemId)
    {
      if (wellKnownBinary == null)
        throw new ArgumentNullException("wellKnownBinary");
    
      DbGeometry mysqlValue = DbGeometry.FromBinary(wellKnownBinary, coordinateSystemId);

      return GeometryFromProviderValue(mysqlValue);
    }

    public override DbGeometry GeometryFromBinary(byte[] wellKnownBinary)
    {
      if (wellKnownBinary == null)
        throw new ArgumentNullException("wellKnownBinary");

      DbGeometry mysqlValue = DbGeometry.FromBinary(wellKnownBinary);

      return GeometryFromProviderValue(mysqlValue);
    }

    public override DbGeometry GeometryFromGml(string geometryMarkup, int coordinateSystemId)
    {
          throw new NotImplementedException();
    }

    public override DbGeometry GeometryFromGml(string geometryMarkup)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryFromProviderValue(object providerValue)
    {
      if (providerValue == null)
        throw new ArgumentNullException("provider value");

      var myGeom = new MySqlGeometry();

      if (MySqlGeometry.TryParse(providerValue.ToString(), out myGeom))      
       return DbGeometry.FromText(myGeom.GetWKT(), myGeom.SRID.Value);
      else
        return null;      
    }

    public override DbGeometry GeometryFromText(string wellKnownText, int coordinateSystemId)
    {
      if (String.IsNullOrEmpty(wellKnownText))
        throw new ArgumentNullException("wellKnownText");

      var geomValue = DbGeometry.FromText(wellKnownText, coordinateSystemId);

      var mysqlValue = GeometryFromProviderValue(geomValue);

      return mysqlValue;
    }

    public override DbGeometry GeometryFromText(string wellKnownText)
    {
      if (String.IsNullOrEmpty(wellKnownText) == null)
        throw new ArgumentNullException("wellKnownText");

      var geomValue = DbGeometry.FromText(wellKnownText);
      
      var mysqlValue = GeometryFromProviderValue(geomValue);

      return mysqlValue;

    }

    public override DbGeometry GeometryLineFromBinary(byte[] lineWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryLineFromText(string lineWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryMultiLineFromBinary(byte[] multiLineWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryMultiLineFromText(string multiLineWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryMultiPointFromBinary(byte[] multiPointWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryMultiPointFromText(string multiPointWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryMultiPolygonFromBinary(byte[] multiPolygonWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryMultiPolygonFromText(string multiPolygonKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryPointFromBinary(byte[] pointWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryPointFromText(string pointWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryPolygonFromBinary(byte[] polygonWellKnownBinary, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GeometryPolygonFromText(string polygonWellKnownText, int coordinateSystemId)
    {
        throw new System.NotImplementedException();
    }

    public override double? GetArea(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");      

       var mysqlValue = GeometryFromProviderValue(geometryValue);
       return mysqlValue.Area;
    }

    public override double? GetArea(DbGeography geographyValue)
    {
          throw new NotImplementedException();
    }

    public override DbGeometry GetBoundary(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Boundary;
    }

    public override DbGeometry GetCentroid(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Centroid;
    }

    public override DbGeometry GetConvexHull(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.ConvexHull;
    }

    public override int GetCoordinateSystemId(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.CoordinateSystemId;
    }

    public override int GetCoordinateSystemId(DbGeography geographyValue)
    {
        throw new NotImplementedException();
    }

    public override int GetDimension(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Dimension;
    }

    public override int GetDimension(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override int? GetElementCount(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.ElementCount;
    }

    public override int? GetElementCount(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override double? GetElevation(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Elevation;
    }

    public override double? GetElevation(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GetEndPoint(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.EndPoint;
    }

    public override DbGeography GetEndPoint(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GetEnvelope(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Envelope;
    }

    public override DbGeometry GetExteriorRing(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.ExteriorRing;
    }

    public override int? GetInteriorRingCount(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.InteriorRingCount;
    }

    public override bool? GetIsClosed(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.IsClosed;
    }

    public override bool? GetIsClosed(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override bool GetIsEmpty(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.IsEmpty;
    }

    public override bool GetIsEmpty(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override bool? GetIsRing(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.IsRing;
    }

    public override bool GetIsSimple(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.IsSimple;
    }

    public override bool GetIsValid(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.IsValid;
    }

    public override double? GetLatitude(DbGeography geographyValue)
    {
          throw new NotImplementedException();
    }

    public override double? GetLength(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Length;
    }

    public override double? GetLength(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override double? GetLongitude(DbGeography geographyValue)
    {
        throw new NotImplementedException();
    }

    public override double? GetMeasure(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.Measure;
    }

    public override double? GetMeasure(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override int? GetPointCount(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.PointCount;
    }

    public override int? GetPointCount(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GetPointOnSurface(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.PointOnSurface;
    }

    public override string GetSpatialTypeName(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.SpatialTypeName;
    }

    public override string GetSpatialTypeName(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override DbGeometry GetStartPoint(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.StartPoint;
    }

    public override DbGeography GetStartPoint(DbGeography geographyValue)
    {
        throw new System.NotImplementedException();
    }

    public override double? GetXCoordinate(DbGeometry geometryValue)
    {
      if (geometryValue == null)          
        throw new ArgumentNullException("geometryValue");

      var providerValue = new MySqlGeometry();
      MySqlGeometry.TryParse(geometryValue.ProviderValue.ToString(), out providerValue);
      return providerValue.XCoordinate;
    }

    public override double? GetYCoordinate(DbGeometry geometryValue)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      var providerValue = new MySqlGeometry();
      MySqlGeometry.TryParse(geometryValue.ProviderValue.ToString(), out providerValue);
      return providerValue.YCoordinate;
    }

    public override DbGeometry InteriorRingAt(DbGeometry geometryValue, int index)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");
      
      var mysqlValue = GeometryFromProviderValue(geometryValue);

      return mysqlValue.InteriorRingAt(index);
    }

    public override DbGeometry Intersection(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Intersection(mysqlOtherValue);
    }

    public override DbGeography Intersection(DbGeography geographyValue, DbGeography otherGeography)
    {
        throw new System.NotImplementedException();
    }

    public override bool Intersects(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Intersects(mysqlOtherValue);
     
    }

    public override bool Intersects(DbGeography geographyValue, DbGeography otherGeography)
    {
        throw new System.NotImplementedException();
    }

    public override bool Overlaps(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");
      
      
      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Overlaps(mysqlOtherValue);
    }

    public override DbGeometry PointAt(DbGeometry geometryValue, int index)
    {
       throw new System.NotImplementedException();
    }

    public override DbGeography PointAt(DbGeography geographyValue, int index)
    {
        throw new System.NotImplementedException();
    }

    public override bool Relate(DbGeometry geometryValue, DbGeometry otherGeometry, string matrix)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      if (String.IsNullOrEmpty(matrix))
        throw new ArgumentNullException("matrix");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Relate(mysqlOtherValue, matrix);
    }

    public override bool SpatialEquals(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.SpatialEquals(mysqlOtherValue);
    }

    public override bool SpatialEquals(DbGeography geographyValue, DbGeography otherGeography)
    {
          throw new NotImplementedException();
    }

    public override DbGeometry SymmetricDifference(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.SymmetricDifference(mysqlOtherValue);
    }

    public override DbGeography SymmetricDifference(DbGeography geographyValue, DbGeography otherGeography)
    {
        throw new System.NotImplementedException();
    }

    public override bool Touches(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);

      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Touches(mysqlOtherValue);
    }

    public override DbGeometry Union(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);
      
      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);

      return mysqlValue.Union(mysqlOtherValue);
    }

    public override DbGeography Union(DbGeography geographyValue, DbGeography otherGeography)
    {
        throw new System.NotImplementedException();
    }

    public override bool Within(DbGeometry geometryValue, DbGeometry otherGeometry)
    {
      if (geometryValue == null)
        throw new ArgumentNullException("geometryValue");

      if (otherGeometry == null)
        throw new ArgumentNullException("otherGeometry");

      var mysqlValue = GeometryFromProviderValue(geometryValue);
      var mysqlOtherValue = GeometryFromProviderValue(otherGeometry);
      return  mysqlValue.Within(mysqlOtherValue);      
    }   

    #endregion
  }
}
