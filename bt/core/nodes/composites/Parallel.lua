
-----------------------------------------------------------------------------------------
-- Composites
-- Parallel

-- Execute all child nodes once but simultaneously and return Success or Failure depending
-- on the selected ParallelPolicy.\n
-- If set to Dynamic, child nodes are repeated until the Policy set is met, or until all 
-- children have had a chance to complete at least once.
-----------------------------------------------------------------------------------------

local Parallel = bt.Class("Parallel",bt.BTComposite)
bt.Parallel = Parallel

ParallelPolicy = 
{
    FirstFailure            = 0,
    FirstSuccess            = 1,
    FirstSuccessOrFailure   = 2,
}
        
function Parallel:ctor()
    bt.BTComposite.ctor(self)
    self.name = "Parallel"
    self.dynamic = false
    self.policy = ParallelPolicy.FirstFailure
    self.finishedConnections = {}
end

function Parallel:init(jsonData)
    if jsonData.dynamic then
        self.dynamic = jsonData.dynamic
    end
    if jsonData.policy then
        self.policy = ParallelPolicy[jsonData.policy]
    end
end

function Parallel:onExecute(agent,blackboard)
    local defferedStatus = bt.Status.Resting
    for k,connection in pairs(self.outConnections) do
        while true do
            if not self.dynamic and self:isContainInFinished(connection) then
                break
            end
            if connection.status ~= bt.Status.Running and self:isContainInFinished(connection) then
                connection:reset(true)
            end
            self.status = connection:execute(agent,blackboard)
            if defferedStatus == bt.Status.Resting then
                if self.status == bt.Status.Failure and (
                   self.policy == ParallelPolicy.FirstFailure or 
                   self.policy == ParallelPolicy.FirstSuccessOrFailure) then
                    defferedStatus = bt.Status.Failure
                end
                if self.status == bt.Status.Success and (
                   self.policy == ParallelPolicy.FirstSuccess or 
                   self.policy == ParallelPolicy.FirstSuccessOrFailure) then
                    defferedStatus = bt.Status.Success
                end
            end
            if self.status ~= bt.Status.Running and self:isContainInFinished(connection) then
                table.insert(self.finishedConnections,connection)
            end
            break
        end
    end
    if defferedStatus ~= bt.Status.Resting then
        self:resetRunning()
        return defferedStatus
    end
    if #self.finishedConnections == #self.outConnections then
        self:resetRunning()
        if self.policy == ParallelPolicy.FirstFailure then
            return bt.Status.Success
        elseif self.policy == ParallelPolicy.FirstSuccess then
            return bt.Status.Failure
        end
    end
    return bt.Status.Running
end

function Parallel:isContainInFinished(connection)
    for k,v in pairs(self.finishedConnections) do
        if v == connection then
            return true
        end
    end
    return false
end

function Parallel:onReset()
    self.finishedConnections = {}
end

function Parallel:resetRunning()
    for k,connection in pairs(self.outConnections) do
        if connection.status == bt.Status.Running then
            connection:reset(true)
        end
    end
end

function Parallel:destroy()
    self.finishedConnections = nil
end