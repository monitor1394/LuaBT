
-----------------------------------------------------------------------------------------
-- Composites
-- Selector

-- Execute the child nodes in order or randonly until the first that returns Success 
-- and return Success as well. If none returns Success, then returns Failure.\n
-- If is Dynamic, then higher priority children Status are revaluated and if one returns 
-- Success the Selector will select that one and bail out immediately in Success too
-----------------------------------------------------------------------------------------

local Selector = bt.Class("Selector",bt.BTComposite)
bt.Selector = Selector

function Selector:ctor()
    bt.BTComposite.ctor(self)
    self.name = "Selector"
    self.dynamic = false
    self.random = false
    self.lastRunningNodeIndex = 1
end

function Selector:init(jsonData)
    if jsonData.dynamic then
        self.dynamic = jsonData.dynamic
    end
    if jsonData.random then
        self.random = jsonData.random
    end
end

function Selector:onExecute(agent,blackboard)
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
        elseif self.status == bt.Status.Success then
            if self.dynamic and i < self.lastRunningNodeIndex then
                for j = i + 1,self.lastRunningNodeIndex do 
                    self.outConnections[j]:reset(true)
                end
            end
            return bt.Status.Success
        end
    end
    return bt.Status.Failure
end

function Selector:onReset()
    self.lastRunningNodeIndex = 1
    if self.random then 
        self:shuffle(self.outConnections)
    end
end

function Selector:onGraphStarted()
    self:onReset()
end

function Selector:shuffle(list)
    local size = #list
    for i = 1,size do
        local j = math.random( i,size)
        list[i],list[j] = list[j],list[i]
    end
end