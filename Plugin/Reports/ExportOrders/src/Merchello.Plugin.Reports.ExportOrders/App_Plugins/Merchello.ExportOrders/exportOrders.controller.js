(function (controllers, undefined) {

    /**
     * @ngdoc controller
     * @name Merchello.Plugins.Reports.ExportOrders
     * @function
     * 
     * @description
     * The controller for the reports Export Orders page
     */
    controllers.ExportOrdersController = function ($scope, merchelloPluginReportOrderExportService, queryDisplayBuilder) {

        $scope.loaded = false;
        $scope.preValuesLoaded = true;

        $scope.exportStatus = "";
        $scope.itemsPerPage = 0;
        $scope.totalItems = 0;
        $scope.filterStartDate = '';
        $scope.filterEndDate = '';
        $scope.currentFilters = [];

        // exposed methods
        $scope.filterWithDates = filterWithDates;
        $scope.exportOrders = exportOrders;

        /**
         * @ngdoc method
         * @name init
         * @function
         *
         * @description
         * Initializes the controller
         */
        function init() {
            setDefaultDates(new Date());
            $scope.loaded = true;
        }

        /**
         * @ngdoc method
         * @name exportOrders
         * @function
         *
         * @description
         * Requests order data download
         */
        function exportOrders() {
            $scope.exportStatus = "Exporting Orders!";
            var query = buildQueryDates($scope.filterStartDate, $scope.filterEndDate);
            console.info(query);
            var promise = merchelloPluginReportOrderExportService.getOrdersByDateRange(query);
            promise.then(function (data) {
                var element = angular.element('<a/>');
                element.attr({
                    href: 'data:attachment/csv;charset=utf-8,' + encodeURI(data),
                    target: '_blank',
                    download: 'orders.csv'
                })[0].click();
            });
        }

        $scope.finishDownload = function () {
            $scope.exportStatus = "";
        }

        /**
         * @ngdoc method
         * @name buildQueryDates
         * @function
         *
         * @description
         * Perpares a new query object for passing to the ApiController
         */
        function buildQueryDates(startDate, endDate) {

            var query = queryDisplayBuilder.createDefault();

            if (startDate === undefined && endDate === undefined) {
                $scope.currentFilters = [];
            } else {
                if (Date.parse(startDate) > Date.parse(endDate)) {
                    var temp = startDate;
                    startDate = endDate;
                    endDate = temp;
                    $scope.filterStartDate = startDate;
                    $scope.filterEndDate = endDate;
                }
                query.addInvoiceDateParam($scope.filterStartDate, 'start');
                query.addInvoiceDateParam($scope.filterEndDate, 'end');
            }

            $scope.filterStartDate = startDate;
            query.currentPage = 0;
            query.itemsPerPage = 25;
            query.sortBy = 'invoiceDate';
            query.sortDirection = 'desc';

            return query;
        }

        /**
         * @ngdoc method
         * @name filterWithDates
         * @function
         *
         * @description
         * Loads a sales by item report filtered by a date range
         */
        function filterWithDates(filterStartDate, filterEndDate) {
            //$scope.loaded = false;
            var query = buildQueryDates(filterStartDate, filterEndDate);
            //console.info(query);
            //exportOrders();
        }

        /**
         * @ngdoc method
         * @name setDefaultDates
         * @function
         *
         * @description
         * Sets the default dates
         */
        function setDefaultDates(actual) {
            var month = actual.getMonth() == 0 ? 11 : actual.getMonth() - 1;
            var start = new Date(actual.getFullYear(), month, actual.getDate());
            var end = new Date(actual.getFullYear(), actual.getMonth(), actual.getDate());
            $scope.filterStartDate = start.toLocaleDateString();
            $scope.filterEndDate = end.toLocaleDateString();
        }

        init();
    };


    angular.module("umbraco").controller("Merchello.Plugins.Reports.ExportOrders", ['$scope', 'merchelloPluginReportOrderExportService', 'queryDisplayBuilder', merchello.Controllers.ExportOrdersController]);


}(window.merchello.Controllers = window.merchello.Controllers || {}));
