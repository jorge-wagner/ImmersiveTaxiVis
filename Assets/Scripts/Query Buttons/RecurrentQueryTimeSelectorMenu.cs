using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecurrentQueryTimeSelectorMenu : MonoBehaviour
{
    public QueryButtonsController qbc;
    public QuerySubmenu qsm;

    public GameObject Y2011, Y2012, Y2013, Y2014, Y2015, January, February, March, April, May, June, July, August, September, October, November, December, 
        Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday, 
        H0, H1, H2, H3, H4, H5, H6, H7, H8, H9, H10, H11, H12, H13, H14, H15, H16, H17, H18, H19, H20, H21, H22, H23;


    public void ApplySelection()
    {
        // Generate lists 

        List<int> years = new List<int>();
        List<int> months = new List<int>();
        List<DayOfWeek> daysOfTheWeek = new List<DayOfWeek>();
        List<int> hours = new List<int>();

        if (Y2011.GetComponent<SwitchBackplateVisual>().state)
            years.Add(2011);
        if (Y2012.GetComponent<SwitchBackplateVisual>().state)
            years.Add(2012);
        if (Y2013.GetComponent<SwitchBackplateVisual>().state)
            years.Add(2013);
        if (Y2014.GetComponent<SwitchBackplateVisual>().state)
            years.Add(2014);
        if (Y2015.GetComponent<SwitchBackplateVisual>().state)
            years.Add(2015);
        if (years.Count == 0) // if no selections, consider all are selected
            years.AddRange(new int[] { 2011, 2012, 2013, 2014, 2015 });

        if (January.GetComponent<SwitchBackplateVisual>().state)
            months.Add(1);
        if (February.GetComponent<SwitchBackplateVisual>().state)
            months.Add(2);
        if (March.GetComponent<SwitchBackplateVisual>().state)
            months.Add(3);
        if (April.GetComponent<SwitchBackplateVisual>().state)
            months.Add(4);
        if (May.GetComponent<SwitchBackplateVisual>().state)
            months.Add(5);
        if (June.GetComponent<SwitchBackplateVisual>().state)
            months.Add(6);
        if (July.GetComponent<SwitchBackplateVisual>().state)
            months.Add(7);
        if (August.GetComponent<SwitchBackplateVisual>().state)
            months.Add(8);
        if (September.GetComponent<SwitchBackplateVisual>().state)
            months.Add(9);
        if (October.GetComponent<SwitchBackplateVisual>().state)
            months.Add(10);
        if (November.GetComponent<SwitchBackplateVisual>().state)
            months.Add(11);
        if (December.GetComponent<SwitchBackplateVisual>().state)
            months.Add(12);
        if (months.Count == 0) // if no selections, consider all are selected
            months.AddRange(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });

        if (Monday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Monday);
        if (Tuesday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Tuesday);
        if (Wednesday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Wednesday);
        if (Thursday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Thursday);
        if (Friday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Friday);
        if (Saturday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Saturday);
        if (Sunday.GetComponent<SwitchBackplateVisual>().state)
            daysOfTheWeek.Add(DayOfWeek.Sunday);
        if (daysOfTheWeek.Count == 0) // if no selections, consider all are selected
            daysOfTheWeek.AddRange(new DayOfWeek[] {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday});

        if (H0.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(0);
        if (H1.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(1);
        if (H2.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(2);
        if (H3.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(3);
        if (H4.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(4);
        if (H5.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(5);
        if (H6.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(6);
        if (H7.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(7);
        if (H8.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(8);
        if (H9.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(9);
        if (H10.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(10);
        if (H11.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(11);
        if (H12.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(12);
        if (H13.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(13);
        if (H14.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(14);
        if (H15.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(15);
        if (H16.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(16);
        if (H17.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(17);
        if (H18.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(18);
        if (H19.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(19);
        if (H20.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(20);
        if (H21.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(21);
        if (H22.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(22);
        if (H23.GetComponent<SwitchBackplateVisual>().state)
            hours.Add(23);
        if (hours.Count == 0) // if no selections, consider all are selected
            hours.AddRange(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 });

        if(qbc !=null && qbc.myQuery != null)
        {
            if(qbc.myQuery is RecurrentQuery)
                qbc.myQuery.qm.EditRecurrentQuery((RecurrentQuery)qbc.myQuery, years, months, daysOfTheWeek, hours);
            else if (qbc.myQuery is AtomicQuery)
                qbc.myQuery.qm.TransformQueryIntoRecurrentQuery((AtomicQuery)qbc.myQuery, years, months, daysOfTheWeek, hours);
            else if (qbc.myQuery is DirectionalQuery)
                qbc.myQuery.qm.TransformDQSubqueriesIntoRecurrent((DirectionalQuery)qbc.myQuery, years, months, daysOfTheWeek, hours);
        }
        else
        {
            if (qsm)
            {
                qsm.sm.qm.TransformAllQueriesIntoRecurrentQueries(years, months, daysOfTheWeek, hours);
                qsm.SwitchRecurrentSelectionModeOff();
            }
        }

    }

    public void LoadPreselections(List<int> years, List<int> months, List<DayOfWeek> daysOfTheWeek, List<int> hours)
    {
        if (years.Contains(2015) && !Y2015.GetComponent<SwitchBackplateVisual>().state)
            Y2015.GetComponent<SwitchBackplateVisual>().switchVisual();

        if(months.Count == 12) // if all months are selected, no buttons are highlighted
        {
            if (January.GetComponent<SwitchBackplateVisual>().state)
                January.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (February.GetComponent<SwitchBackplateVisual>().state)
                February.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (March.GetComponent<SwitchBackplateVisual>().state)
                March.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (April.GetComponent<SwitchBackplateVisual>().state)
                April.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (May.GetComponent<SwitchBackplateVisual>().state)
                May.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (June.GetComponent<SwitchBackplateVisual>().state)
                June.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (July.GetComponent<SwitchBackplateVisual>().state)
                July.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (August.GetComponent<SwitchBackplateVisual>().state)
                August.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (September.GetComponent<SwitchBackplateVisual>().state)
                September.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (October.GetComponent<SwitchBackplateVisual>().state)
                October.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (November.GetComponent<SwitchBackplateVisual>().state)
                November.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (December.GetComponent<SwitchBackplateVisual>().state)
                December.GetComponent<SwitchBackplateVisual>().switchVisual();
        }
        else
        {
            if (months.Contains(1) && !January.GetComponent<SwitchBackplateVisual>().state)
                January.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(2) && !February.GetComponent<SwitchBackplateVisual>().state)
                February.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(3) && !March.GetComponent<SwitchBackplateVisual>().state)
                March.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(4) && !April.GetComponent<SwitchBackplateVisual>().state)
                April.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(5) && !May.GetComponent<SwitchBackplateVisual>().state)
                May.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(6) && !June.GetComponent<SwitchBackplateVisual>().state)
                June.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(7) && !July.GetComponent<SwitchBackplateVisual>().state)
                July.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(8) && !August.GetComponent<SwitchBackplateVisual>().state)
                August.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(9) && !September.GetComponent<SwitchBackplateVisual>().state)
                September.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(10) && !October.GetComponent<SwitchBackplateVisual>().state)
                October.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(11) && !November.GetComponent<SwitchBackplateVisual>().state)
                November.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (months.Contains(12) && !December.GetComponent<SwitchBackplateVisual>().state)
                December.GetComponent<SwitchBackplateVisual>().switchVisual();
        }

        if (daysOfTheWeek.Count == 7) // if all are selected, no buttons are highlighted
        {
            if (Monday.GetComponent<SwitchBackplateVisual>().state)
                Monday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (Tuesday.GetComponent<SwitchBackplateVisual>().state)
                Tuesday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (Wednesday.GetComponent<SwitchBackplateVisual>().state)
                Wednesday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (Thursday.GetComponent<SwitchBackplateVisual>().state)
                Thursday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (Friday.GetComponent<SwitchBackplateVisual>().state)
                Friday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (Saturday.GetComponent<SwitchBackplateVisual>().state)
                Saturday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (Sunday.GetComponent<SwitchBackplateVisual>().state)
                Sunday.GetComponent<SwitchBackplateVisual>().switchVisual();
        }
        else
        {
            if (daysOfTheWeek.Contains(DayOfWeek.Monday) && !Monday.GetComponent<SwitchBackplateVisual>().state)
                Monday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (daysOfTheWeek.Contains(DayOfWeek.Tuesday) && !Tuesday.GetComponent<SwitchBackplateVisual>().state)
                Tuesday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (daysOfTheWeek.Contains(DayOfWeek.Wednesday) && !Wednesday.GetComponent<SwitchBackplateVisual>().state)
                Wednesday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (daysOfTheWeek.Contains(DayOfWeek.Thursday) && !Thursday.GetComponent<SwitchBackplateVisual>().state)
                Thursday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (daysOfTheWeek.Contains(DayOfWeek.Friday) && !Friday.GetComponent<SwitchBackplateVisual>().state)
                Friday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (daysOfTheWeek.Contains(DayOfWeek.Saturday) && !Saturday.GetComponent<SwitchBackplateVisual>().state)
                Saturday.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (daysOfTheWeek.Contains(DayOfWeek.Sunday) && !Sunday.GetComponent<SwitchBackplateVisual>().state)
                Sunday.GetComponent<SwitchBackplateVisual>().switchVisual();
        }

        if (hours.Count == 24) // if all are selected, no buttons are highlighted
        {
            if (H0.GetComponent<SwitchBackplateVisual>().state)
                H0.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H1.GetComponent<SwitchBackplateVisual>().state)
                H1.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H2.GetComponent<SwitchBackplateVisual>().state)
                H2.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H3.GetComponent<SwitchBackplateVisual>().state)
                H3.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H4.GetComponent<SwitchBackplateVisual>().state)
                H4.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H5.GetComponent<SwitchBackplateVisual>().state)
                H5.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H6.GetComponent<SwitchBackplateVisual>().state)
                H6.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H7.GetComponent<SwitchBackplateVisual>().state)
                H7.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H8.GetComponent<SwitchBackplateVisual>().state)
                H8.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H9.GetComponent<SwitchBackplateVisual>().state)
                H9.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H10.GetComponent<SwitchBackplateVisual>().state)
                H10.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H11.GetComponent<SwitchBackplateVisual>().state)
                H11.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H12.GetComponent<SwitchBackplateVisual>().state)
                H12.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H13.GetComponent<SwitchBackplateVisual>().state)
                H13.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H14.GetComponent<SwitchBackplateVisual>().state)
                H14.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H15.GetComponent<SwitchBackplateVisual>().state)
                H15.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H16.GetComponent<SwitchBackplateVisual>().state)
                H16.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H17.GetComponent<SwitchBackplateVisual>().state)
                H17.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H18.GetComponent<SwitchBackplateVisual>().state)
                H18.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H19.GetComponent<SwitchBackplateVisual>().state)
                H19.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H20.GetComponent<SwitchBackplateVisual>().state)
                H20.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H21.GetComponent<SwitchBackplateVisual>().state)
                H21.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H22.GetComponent<SwitchBackplateVisual>().state)
                H22.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (H23.GetComponent<SwitchBackplateVisual>().state)
                H23.GetComponent<SwitchBackplateVisual>().switchVisual();
        }
        else
        {
            if (hours.Contains(0) && !H0.GetComponent<SwitchBackplateVisual>().state)
                H0.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(1) && !H1.GetComponent<SwitchBackplateVisual>().state)
                H1.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(2) && !H2.GetComponent<SwitchBackplateVisual>().state)
                H2.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(3) && !H3.GetComponent<SwitchBackplateVisual>().state)
                H3.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(4) && !H4.GetComponent<SwitchBackplateVisual>().state)
                H4.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(5) && !H5.GetComponent<SwitchBackplateVisual>().state)
                H5.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(6) && !H6.GetComponent<SwitchBackplateVisual>().state)
                H6.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(7) && !H7.GetComponent<SwitchBackplateVisual>().state)
                H7.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(8) && !H8.GetComponent<SwitchBackplateVisual>().state)
                H8.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(9) && !H9.GetComponent<SwitchBackplateVisual>().state)
                H9.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(10) && !H10.GetComponent<SwitchBackplateVisual>().state)
                H10.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(11) && !H11.GetComponent<SwitchBackplateVisual>().state)
                H11.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(12) && !H12.GetComponent<SwitchBackplateVisual>().state)
                H12.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(13) && !H13.GetComponent<SwitchBackplateVisual>().state)
                H13.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(14) && !H14.GetComponent<SwitchBackplateVisual>().state)
                H14.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(15) && !H15.GetComponent<SwitchBackplateVisual>().state)
                H15.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(16) && !H16.GetComponent<SwitchBackplateVisual>().state)
                H16.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(17) && !H17.GetComponent<SwitchBackplateVisual>().state)
                H17.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(18) && !H18.GetComponent<SwitchBackplateVisual>().state)
                H18.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(19) && !H19.GetComponent<SwitchBackplateVisual>().state)
                H19.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(20) && !H20.GetComponent<SwitchBackplateVisual>().state)
                H20.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(21) && !H21.GetComponent<SwitchBackplateVisual>().state)
                H21.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(22) && !H22.GetComponent<SwitchBackplateVisual>().state)
                H22.GetComponent<SwitchBackplateVisual>().switchVisual();
            if (hours.Contains(23) && !H23.GetComponent<SwitchBackplateVisual>().state)
                H23.GetComponent<SwitchBackplateVisual>().switchVisual();
        }
        

    }

    public void Cancel()
    {
        if(qbc) 
            qbc.SwitchRecurrentSelectionModeOff();
        if (qsm)
            qsm.SwitchRecurrentSelectionModeOff();
    }

}
