-----------------------------------------------------------------------------------------
-- Composites
-- Sequencer

-- Execute the child nodes in order or randonly and return Success if all children return 
-- Success, else return Failure.
-- If is Dynamic, higher priority child status is revaluated. 
-- If a child returns Failure the Sequencer will bail out immediately in Failure too.
-----------------------------------------------------------------------------------------

local Sequencer = bt.Class("Sequencer",bt.BTComposite)
bt.Sequencer = Sequencer

function Sequencer:ctor()
    bt.BTComposite.ctor(self)
    self.name = "Sequencer"
    self.dynamic = false
    self.random = false
    self.lastRunningNodeIndex = 1
end

function Sequencer:init(jsonData)
    if jsonData.dynamic then
        self.dynamic = jsonData.dynamic
    end
    if jsonData.random then
        self.random = jsonData.random
    end
end

function Sequencer:onExecute(agent,blackboard)
    local startIndex = self.lastRunningNodeIndex
    if self.dynamic then
        startIndex = 1
    end
    local size = #self.outConnections
    for i = startIndex,size do
        self.status = self.outConnections[i]:execute(agent,blackboard)
        if self.status == bt.Status.Running then
            if self.dynamic and i < self.lastRunningNodeIndex then
                self.outConnections[self.lastRunningNodeIndex]:reset(true)
            end
            self.lastRunningNodeIndex = i
            return bt.Status.Running
        elseif self.status == bt.Status.Failure then
            if self.dynamic and i < self.lastRunningNodeIndex then
                for j = i + 1,self.lastRunningNodeIndex do 
                    self.outConnections[j]:reset(true)
                end
            end
            return bt.Status.Failure
        end
    end
    return bt.Status.Success
end

function Sequencer:onReset()
    self.lastRunningNodeIndex = 1
    if self.random then 
        self:shuffle(self.outConnections)
    end
end

function Sequencer:onGraphStarted()
    self:onReset()
end

function Sequencer:shuffle(list)
    local size = #list
    for i = 1,size do
        local j = math.random( i,size)
        list[i],list[j] = list[j],list[i]
    end
end