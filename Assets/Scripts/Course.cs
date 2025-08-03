using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CourseSearchResponse
{
    public List<Course> courses;
}

[System.Serializable]
public class Course
{
    public string course_id;
    public string? club_name;
    public string course_name;
    public string? address;
    public string? city;
    public string? state;
    public string? country;
    public float latitude;
    public float longitude;
    public TeeBox tees;
}

// [System.Serializable]
// public class Location
// {
//     public string address;
//     public string city;
//     public string state;
//     public string country;
//     public float latitude;
//     public float longitude;
// }


[System.Serializable]
public class TeeBox
{
    public string teebox_id;
    public string name;
    public string? gender;
    public float? course_rating;
    public int? slope_rating;
    public int? total_yards;
    public int? numholes;
    public int? par;
    public float? front_course_rating;
    public int? front_slope_rating;
    public float? back_course_rating;
    public int? back_slope_rating;
    public List<Hole> holes;
}

[System.Serializable]
public class Hole
{
    public int hole_num;
    public int par;
    public int yardage;
    public int? handicap;
    public float? gps_lat;
    public float? gps_lon;
}