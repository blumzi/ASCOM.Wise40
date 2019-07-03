//'use strict';
//var http = require('http');
//var port = process.env.PORT || 1337;

import { GoogleCharts } from 'google-charts';
//Load the charts library with a callback
GoogleCharts.load(drawChart);

//var fs = require("fs");


//http.createServer(function (req, res) {
//    res.writeHead(200, { 'Content-Type': 'text/plain' });
//    res.end('Hello World\n');
//}).listen(port);

function drawChart() {
    loadFile('C18', 'Humidity');
}

function loadFile(station, sensor) {
    var dirname = 'c:/Wise40/Logs/2019-07-01/Weather/';
    var lineReader = require('readline').createInterface({
        input: fs.createReadStream(dirname + sensor + '/' + station + '.dat')
    });

    // Define the chart to be drawn.
    var data = new google.visualization.DataTable();
    data.addColumn('time', 'Time');
    data.addColumn('number', station);

    lineReader.on('line', function (line) {
        var array = line.split(" ");

        data.AddRow([array[0], array[1]])
    });

    var options = {
        chart: {
            title: 'Box Office Earnings in First Two Weeks of Opening',
            subtitle: 'in millions of dollars (USD)'
        },
        width: 900,
        height: 500
    };

    var chart = new google.charts.Line(global.getElementById('linechart_material'));

    chart.draw(data, google.charts.Line.convertOptions(options));
}
