Imports GeoAPI.Geometries
Imports Autodesk.AutoCAD.DatabaseServices
Imports Autodesk.AutoCAD.Geometry

''' <summary>
''' Reads features based on JTS model and creates their AutoCAD representation
''' using single floating precision model.
''' Created AutoCAD entities are not database resident, it's up to you to commit
''' them to the existing <c>Database</c> using <c>Transaction</c>.
''' </summary>
''' <remarks>
''' This library references two Autodesk libraries being part of managed ObjectARX.
''' Referenced libraries are <c>acdbmgd.dll</c> and <c>acmgd.dll</c> which may be found
''' in the root installation folder of the targeted Autodesk platform/vertical.
''' </remarks>
Public Class DwgWriter

    Private m_GeometryFactory As IGeometryFactory

#Region " GeometryFactory "

    ''' <summary>
    ''' Returns current <see cref="GeometryFactory"/> used to build geometries.
    ''' </summary>
    ''' <value></value>
    ''' <returns>Current <see cref="GeometryFactory"/> instance.</returns>
    ''' <remarks>
    ''' If there's no <see cref="GeometryFactory"/> set within class constructor,
    ''' a <c>Default</c> factory will be automatically instantiated. Otherwise,
    ''' user-supplied <see cref="GeometryFactory"/> will be used during geometry
    ''' building process.
    ''' </remarks>
    Public ReadOnly Property GeometryFactory() As IGeometryFactory
        Get
            If m_GeometryFactory Is Nothing Then
                m_GeometryFactory = GeoAPI.GeometryServiceProvider.Instance.CreateGeometryFactory()
            End If
            Return m_GeometryFactory
        End Get
    End Property

#End Region

#Region " PrecisionModel "

    ''' <summary>
    ''' Returns current <see cref="PrecisionModel"/> of the coordinates within any
    ''' processed <see cref="Geometry"/>.
    ''' </summary>
    ''' <value></value>
    ''' <returns>Current <see cref="GeometryFactory.PrecisionModel"/> instance.</returns>
    ''' <remarks>
    ''' If there's no <see cref="GeometryFactory.PrecisionModel"/> set within class constructor,
    ''' returns default <see cref="GeometryFactory.PrecisionModel"/>. Default precision model is
    ''' <c>Floating</c>, meaning full double precision floating point.
    ''' </remarks>
    Public ReadOnly Property PrecisionModel() As IPrecisionModel
        Get
            Return Me.GeometryFactory.PrecisionModel
        End Get
    End Property

#End Region

#Region " CTOR "

    Sub New()
        Me.New(GeoAPI.GeometryServiceProvider.Instance.CreateGeometryFactory())
    End Sub

    Sub New(ByVal factory As IGeometryFactory)
        m_GeometryFactory = factory
    End Sub

#End Region


#Region " WritePoint3d "

    ''' <summary>
    ''' Returns <see cref="Point3d"/> structure converted from <see cref="Coordinate"/>.
    ''' If <see cref="Coordinate"/> is two-dimensional, collapses <c>Z</c> axis of
    ''' resulting <see cref="Point3d"/> to 0.
    ''' </summary>
    ''' <param name="coordinate">A <see cref="Coordinate"/> structure.</param>
    ''' <returns>A <see cref="Point3d"/> structure.</returns>
    ''' <remarks></remarks>
    Public Function WritePoint3d(ByVal coordinate As Coordinate) As Point3d
        If Not Double.IsNaN(coordinate.Z) Then
            Return New Point3d( _
                Me.PrecisionModel.MakePrecise(coordinate.X), _
                Me.PrecisionModel.MakePrecise(coordinate.Y), _
                Me.PrecisionModel.MakePrecise(coordinate.Z))
        Else
            Return New Point3d( _
                Me.PrecisionModel.MakePrecise(coordinate.X), _
                Me.PrecisionModel.MakePrecise(coordinate.Y), _
                0)
        End If
    End Function

    ''' <summary>
    ''' Returns <see cref="Point3d"/> structure converted from <see cref="Point"/> geometry.
    ''' If <see cref="Point"/> is two-dimensional, collapses <c>Z</c> axis of resulting
    ''' <see cref="Point3d"/> to 0.
    ''' </summary>
    ''' <param name="point">A <see cref="Point"/> geometry.</param>
    ''' <returns>A <see cref="Point3d"/> structure.</returns>
    ''' <remarks></remarks>
    Public Function WritePoint3d(ByVal point As IPoint) As Point3d
        Return Me.WritePoint3d(point.Coordinate)
    End Function

#End Region

#Region " WritePoint2d "

    ''' <summary>
    ''' Returns <see cref="Point2d"/> structure converted from <see cref="Coordinate"/>.
    ''' If <see cref="Coordinate"/> is three-dimensional, clamps resulting <c>Z</c> axis.
    ''' </summary>
    ''' <param name="coordinate">A <see cref="Coordinate"/> structure.</param>
    ''' <returns>A <see cref="Point2d"/> structure.</returns>
    ''' <remarks></remarks>
    Public Function WritePoint2d(ByVal coordinate As Coordinate) As Point2d
        Return New Point2d( _
            Me.PrecisionModel.MakePrecise(coordinate.X), _
            Me.PrecisionModel.MakePrecise(coordinate.Y))
    End Function

    ''' <summary>
    ''' Returns <see cref="Point2d"/> structure converted from <see cref="Point"/> geometry.
    ''' If <see cref="Point"/> is three-dimensional, clamps resulting <c>Z</c> axis.
    ''' </summary>
    ''' <param name="point">A <see cref="Point"/> geometry.</param>
    ''' <returns>A <see cref="Point2d"/> structure.</returns>
    ''' <remarks></remarks>
    Public Function WritePoint2d(ByVal point As IPoint) As Point2d
        Return Me.WritePoint2d(point.Coordinate)
    End Function

#End Region


#Region " WriteDbPoint "

    ''' <summary>
    ''' Returns <see cref="DBPoint"/> entity converted from <see cref="Point"/> geometry.
    ''' </summary>
    ''' <param name="point">A <see cref="Point"/> geometry.</param>
    ''' <returns>A <see cref="DBPoint"/> entity (<c>POINT</c>).</returns>
    ''' <remarks></remarks>
    Public Function WriteDbPoint(ByVal point As IPoint) As DBPoint
        Return New DBPoint(Me.WritePoint3d(point))
    End Function

#End Region

#Region " WritePolyline "

    ''' <summary>
    ''' Returns <see cref="Polyline"/> entity converted from <see cref="LineString"/> geometry.
    ''' </summary>
    ''' <param name="lineString">A <see cref="LineString"/> geometry.</param>
    ''' <returns>A <see cref="Polyline"/> entity (<c>LWPOLYLINE</c>).</returns>
    ''' <remarks>
    ''' If first and last coordinate in the <see cref="LineString"/> coordinate sequence are equal,
    ''' returned <see cref="Polyline"/> is closed. To check whether <see cref="LineString"/> is
    ''' closed, see it's <see cref="LineString.IsClosed"/> property.
    ''' </remarks>
    Public Function WritePolyline(ByVal lineString As ILineString) As Polyline
        Dim geometry As New Polyline
        Dim i As Integer
        For Each coordinate As Coordinate In lineString.Coordinates
            geometry.AddVertexAt(i, New Point2d(coordinate.X, coordinate.Y), 0, 0, 0)
            i += 1
        Next
        geometry.Closed = lineString.StartPoint.EqualsExact(lineString.EndPoint)
        geometry.MinimizeMemory()
        Return geometry
    End Function

    ''' <summary>
    ''' Returns <see cref="Polyline"/> entity converted from <see cref="LinearRing"/> geometry.
    ''' Resulting <see cref="Polyline"/> is always closed.
    ''' </summary>
    ''' <param name="linearRing">A <see cref="LinearRing"/> geometry.</param>
    ''' <returns>A <see cref="Polyline"/> entity (<c>LWPOLYLINE</c>).</returns>
    ''' <remarks></remarks>
    Public Function WritePolyline(ByVal linearRing As ILinearRing) As Polyline
        Dim geometry As New Polyline
        Dim i As Integer
        For Each coordinate As Coordinate In linearRing.Coordinates
            geometry.AddVertexAt(i, Me.WritePoint2d(coordinate), 0, 0, 0)
            i += 1
        Next
        geometry.Closed = True
        geometry.MinimizeMemory()
        Return geometry
    End Function

    ''' <summary>
    ''' Returns <c>1..n</c> collection of <see cref="Polyline"/> entities converted from <see cref="Polygon"/> geometry.
    ''' First <see cref="Polyline"/> in a collection is always <see cref="Polygon.Shell"/>. The rest of
    ''' resulting collection items represent <see cref="Polygon.Holes"/>, if <see cref="Polygon"/> geometry had
    ''' holes (inner boundaries) in first place.
    ''' </summary>
    ''' <param name="polygon">A <see cref="Polygon"/> geometry.</param>
    ''' <returns>Array of <see cref="Polyline"/> entities (<c>LWPOLYLINE</c>s).</returns>
    ''' <remarks></remarks>
    Public Function WritePolyline(ByVal polygon As IPolygon) As Polyline()
        Dim polylines As New List(Of Polyline)

        polylines.Add(Me.WritePolyline(polygon.Shell))

        For Each hole As ILinearRing In polygon.Holes
            polylines.Add(Me.WritePolyline(hole))
        Next

        Return polylines.ToArray
    End Function

#End Region

#Region " WritePolyline3d "

    ''' <summary>
    ''' Returns <see cref="Polyline3d"/> entity converted from <see cref="LineString"/> geometry.
    ''' </summary>
    ''' <param name="lineString">A <see cref="LineString"/> geometry.</param>
    ''' <returns>A <see cref="Polyline3d"/> entity (<c>POLYLINE</c>).</returns>
    ''' <remarks>
    ''' If first and last coordinate in the <see cref="LineString"/> coordinate sequence are equal,
    ''' returned <see cref="Polyline3d"/> is closed. To check whether <see cref="LineString"/> is
    ''' closed, see it's <see cref="LineString.IsClosed"/> property.
    ''' </remarks>
    Public Function WritePolyline3d(ByVal lineString As ILineString) As Polyline3d
        Dim points As New Point3dCollection
        For Each coordinate As Coordinate In lineString.Coordinates
            points.Add(Me.WritePoint3d(coordinate))
        Next
        Return New Polyline3d(Poly3dType.SimplePoly, points, lineString.StartPoint.EqualsExact(lineString.EndPoint))
    End Function

    ''' <summary>
    ''' Returns <see cref="Polyline3d"/> entity converted from <see cref="LinearRing"/> geometry.
    ''' Resulting <see cref="Polyline3d"/> is always closed.
    ''' </summary>
    ''' <param name="linearRing">A <see cref="LinearRing"/> geometry.</param>
    ''' <returns>A <see cref="Polyline3d"/> entity (<c>POLYLINE</c>).</returns>
    ''' <remarks></remarks>
    Public Function WritePolyline3d(ByVal linearRing As ILinearRing) As Polyline3d
        Dim points As New Point3dCollection
        For Each coordinate As Coordinate In linearRing.Coordinates
            points.Add(Me.WritePoint3d(coordinate))
        Next
        Return New Polyline3d(Poly3dType.SimplePoly, points, True)
    End Function

#End Region

#Region " WritePolyline2d "

    ''' <summary>
    ''' Returns <see cref="Polyline2d"/> entity converted from <see cref="LineString"/> geometry.
    ''' </summary>
    ''' <param name="lineString">A <see cref="LineString"/> geometry.</param>
    ''' <returns>A <see cref="Polyline2d"/> ("old-style") entity.</returns>
    ''' <remarks>
    ''' If first and last coordinate in the <see cref="LineString"/> coordinate sequence are equal,
    ''' returned <see cref="Polyline2d"/> is closed. To check whether <see cref="LineString"/> is
    ''' closed, see it's <see cref="LineString.IsClosed"/> property.
    ''' </remarks>
    Public Function WritePolyline2d(ByVal lineString As ILineString) As Polyline2d
        Dim points As New Point3dCollection
        For Each coordinate As Coordinate In lineString.Coordinates
            points.Add(Me.WritePoint3d(coordinate))
        Next
        Return New Polyline2d(Poly2dType.SimplePoly, points, 0, lineString.StartPoint.EqualsExact(lineString.EndPoint), 0, 0, Nothing)
    End Function

    ''' <summary>
    ''' Returns <see cref="Polyline2d"/> entity converted from <see cref="LinearRing"/> geometry.
    ''' Resulting <see cref="Polyline2d"/> is always closed.
    ''' </summary>
    ''' <param name="linearRing">A <see cref="LinearRing"/> geometry.</param>
    ''' <returns>A <see cref="Polyline2d"/> ("old-style") entity.</returns>
    ''' <remarks></remarks>
    Public Function WritePolyline2d(ByVal linearRing As ILinearRing) As Polyline2d
        Dim points As New Point3dCollection
        For Each coordinate As Coordinate In linearRing.Coordinates
            points.Add(Me.WritePoint3d(coordinate))
        Next
        Return New Polyline2d(Poly2dType.SimplePoly, points, 0, True, 0, 0, Nothing)
    End Function

#End Region

#Region " WriteLine "

    ''' <summary>
    ''' Returns <see cref="Line"/> entity converted from <see cref="LineSegment"/> geometry.
    ''' </summary>
    ''' <param name="lineSegment">A <see cref="LineSegment"/> geometry.</param>
    ''' <returns>A <see cref="Line"/> entity (<c>LINE</c>).</returns>
    ''' <remarks></remarks>
    Public Function WriteLine(ByVal lineSegment As NetTopologySuite.Geometries.LineSegment) As Line
        Dim geometry As New Line
        geometry.StartPoint = Me.WritePoint3d(lineSegment.P0)
        geometry.EndPoint = Me.WritePoint3d(lineSegment.P1)
        Return geometry
    End Function

#End Region

    Public Function WriteEntity(ByVal rxClassName As String, ByVal geometry As IGeometry) As Entity
        Select Case rxClassName
            Case "AcDbLine", "AcDbPolyline", "AcDb2dPolyline", "AcDb3dPolyline", "AcDbMline"
                Return Me.WritePolyline(CType(geometry, ILineString))

            Case "AcDbBlockReference", "AcDbPoint"
                Return Me.WriteDbPoint(CType(geometry, IPoint))

            Case Else
                Throw New ArgumentException(String.Format("Geometry conversion from {0} to {1} is not supported.", rxClassName, geometry.GeometryType))
                Return Nothing
        End Select
    End Function

End Class

