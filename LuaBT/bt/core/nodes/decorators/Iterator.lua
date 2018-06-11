
-----------------------------------------------------------------------------------------
-- Decorators
-- Iterator

-- Iterate any type of list and execute the child node for each element in the list. Keeps 
-- iterating until the Termination Condition is met or the whole list is iterated and 
-- return the child node status
-----------------------------------------------------------------------------------------

local Iterator = bt.Class("Iterator",bt.BTDecorator)
bt.Iterator = Iterator
 
TerminationConditions = 
{
    None            = 0,
    FirstSuccess    = 1,
    FirstFailure    = 2,
}
        
function Iterator:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Iterator"
    self.targetList = {}
    self.current = nil
    self.storeIndex = 0
    self.maxInteration = -1
    self.terminationCondition = TerminationConditions.None
    self.resetIndex = true
    self.currentIndex = 1
end

function Iterator:init(jsonData)
    if jsonData.targetList then
        --TODO
    end
    if jsonData.current then
        --TODO
    end
    if jsonData.storeIndex then
        --TODO
    end
    if jsonData.maxIteration then
        self.maxInteration = jsonData.maxIteration._value
    end
    if jsonData.terminationCondition then
        self.terminationCondition = TerminationConditions[jsonData.terminationCondition]
    end
    if jsonData.resetIndex ~= nil then
        self.resetIndex = jsonData.resetIndex
    end
end

function Iterator:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    if #self.targetList <= 0 then
        return bt.Status.Failure
    end
    for i = self.currentIndex,#self.targetList do
        self.current = self.targetList[i]
        self.storeIndex = i
        self.status = decoratedConnection:execute(agent,blackboard)

        if self.status == bt.Status.Success and 
           self.terminationCondition == TerminationConditions.FirstSuccess then
            return bt.Status.Success
        end

        if self.status == bt.Status.Failure and 
           self.terminationCondition == TerminationConditions.FirstFailure then
            return bt.Status.Failure
        end

        if self.status == bt.Status.Running then
            self.currentIndex = i
            return bt.Status.Running
        end

        if self.currentIndex == #self.targetList or self.currentIndex == self.maxInteration then
            if self.resetIndex then
                self.currentIndex = 1
            end
            return self.status
        end

        decoratedConnection:reset(true)
        self.currentIndex = self.currentIndex + 1
    end
    return bt.Status.Running
end

function Iterator:onReset()
    if self.resetIndex then
        self.currentIndex = 1
    end
end

function Iterator:destroy()
    for k,v in pairs(self.targetList) do
        v = nil
    end
    self.targetList = nil
end
