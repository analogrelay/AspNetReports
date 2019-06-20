// Chart colors from c
var chartColors = [
    /* blue */ 'rgb(54, 162, 235)',
    /* yellow */ 'rgb(255, 205, 86)',
    /* orange */ 'rgb(255, 159, 64)',
    /* green */ 'rgb(75, 192, 192)',
    /* purple */ 'rgb(153, 102, 255)',
    /* red */ 'rgb(255, 99, 132)',
];

function renderBurndown(host, burndownData) {
    var lineData = [];
    var barDataSets = [];
    var counter = 0;
    for (var area of burndownData.areas) {
        var areaData = [];
        for (var week of burndownData.weeks) {
            var weekArea = week.areas.find(a => a.label === area);
            areaData.push(weekArea.open);
        }

        barDataSets.unshift({
            label: area,
            backgroundColor: chartColors[counter % chartColors.length],
            data: areaData,
            stack: "Stack 0"
        });
        counter += 1;
    }

    for (var week of burndownData.weeks) {
        var count = 0;
        for (var area of week.areas) {
            count += area.open
        }
        lineData.push(count);
    }

    var labels = burndownData.weeks.map(w => w.week);

    var chart = new Chart(host, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [
                {
                    label: "All Issues",
                    data: lineData,
                    borderColor: 'rgb(201, 203, 207)',
                    backgroundColor: 'rgba(255, 255, 255, 0)',
                    type: 'line'
                },
                ...barDataSets
            ]
        }
    });
}

// Expect the data to be present in window.burndownData
if (typeof window.burndownData !== "object") {
    console.error("Missing burndown data!");
} else {
    var host = document.getElementById("burndown-chart-host");
    if (!host) {
        console.error("Missing burndown chart host!");
    }
    else {
        renderBurndown(host, window.burndownData);
    }
}